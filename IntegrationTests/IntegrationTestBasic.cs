using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace GhostfolioSidekick.IntegrationTests
{
	[Collection("IntegrationTest")]
	public class IntegrationTestBasic : IAsyncLifetime
	{
		private const bool ReuseContainers = false;
		private const string TestContainerHostName = "host.testcontainers.internal";

		private const string Username = "testuser";
		private const string Password = "testpassword";
		private const string Database = "testdb";

		private const int ReditPort = 6379;
		private const int PostgresPort = 5432;
		private const int GhostfolioPort = 3333;

		private PostgreSqlContainer postgresContainer = default!;
		private RedisContainer redisContainer = default!;
		private IContainer ghostfolioContainer = default!;
		private HttpClient httpClient = default!;
		private AuthData? authToken;

		private readonly Dictionary<string, int> AccountsWithExpectedNumbers = new()
		{
			{ "TestAccount1", 2 },
			{ "TestAccount2", 1 },
		};

		[Fact(Timeout = 600000)]
		public async Task CanSetupGhostfolioDependencies()
		{
			// url ghostfolio for debugging:
			var url = new UriBuilder(Uri.UriSchemeHttp, ghostfolioContainer.Hostname, ghostfolioContainer.GetMappedPublicPort(GhostfolioPort)).Uri.ToString();
			authToken.Should().NotBeNull();

			await InitializeSidekick(authToken, url);
		}

		public async Task InitializeAsync()
		{
			INetwork network = new NetworkBuilder()
				.WithCleanUp(true)
				.WithName("ghostfolio-network")
				.Build();

			await InitializePostgresContainer(network).ConfigureAwait(false);
			await InitializeRedisContainer(network).ConfigureAwait(false);
			await InitializeGhostfolioContainer(network).ConfigureAwait(false);

			// Initialize HttpClient.
			httpClient = new HttpClient();

			// Ensure the containers are running.
			EnsureContainersAreRunning();

			// Can access the Ghostfolio container.
			var ghostfolioUri = new UriBuilder(Uri.UriSchemeHttp, ghostfolioContainer.Hostname, ghostfolioContainer.GetMappedPublicPort(GhostfolioPort)).Uri;
			var response = await httpClient.GetAsync(ghostfolioUri).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();

			// Create a new user.
			response = await httpClient.PostAsync($"{ghostfolioUri}api/v1/user", new StringContent("{}")).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
			authToken = await response.Content.ReadFromJsonAsync<AuthData>().ConfigureAwait(false);
		}

		public async Task DisposeAsync()
		{
			// Dispose HttpClient.
			httpClient.Dispose();

			// Stop and dispose the Ghostfolio container.
			await ghostfolioContainer.StopAsync().ConfigureAwait(false);
			await ghostfolioContainer.DisposeAsync().ConfigureAwait(false);

			// Stop and dispose the PostgreSQL container.
			await postgresContainer.StopAsync().ConfigureAwait(false);
			await postgresContainer.DisposeAsync().ConfigureAwait(false);

			// Stop and dispose the Redis container.
			await redisContainer.StopAsync().ConfigureAwait(false);
			await redisContainer.DisposeAsync().ConfigureAwait(false);
		}

		private async Task InitializeSidekick(AuthData authToken, string url)
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", url);
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", authToken.AccessToken);
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", "./Files/");
			Environment.SetEnvironmentVariable("CONFIGURATIONFILE_PATH", "./Files/config.json");

			var testLogger = new TestLogger("Service SyncActivitiesWithGhostfolioTask has executed.");
			var testHost = Program
			.CreateHostBuilder()
			.ConfigureServices((hostContext, services) =>
			{
				services.AddSingleton<ILogger<TimedHostedService>>(testLogger);
			})
			.Build();

			var host = testHost.Services.GetService<IHostedService>();
			var c = new CancellationToken();

			var apiWrapper = testHost.Services.GetRequiredService<IApiWrapper>();

			// Act
			await host!.StartAsync(c);

			while (!testLogger.IsTriggered)
			{
				await Task.Delay(1000);
			}

			// Assert
			await VerifyInstance(apiWrapper);
		}

		private async Task VerifyInstance(IApiWrapper apiWrapper)
		{
			foreach (var item in AccountsWithExpectedNumbers)
			{
				var account = await apiWrapper.GetAccountByName(item.Key);
				account.Should().NotBeNull();

				var activities = await apiWrapper.GetActivitiesByAccount(account!);
				activities.Should().HaveCount(item.Value);
			}
		}

		private async Task InitializePostgresContainer(INetwork network)
		{
			postgresContainer = new PostgreSqlBuilder("postgres:16")
				.WithReuse(ReuseContainers)
				.WithUsername(Username)
				.WithPassword(Password)
				.WithDatabase(Database)
				.WithPortBinding(PostgresPort, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(PostgresPort))
				.WithNetwork(network)
				.WithNetworkAliases("postgrescontainer")
				.Build();
			await postgresContainer.StartAsync().ConfigureAwait(false);

			// Given
			const string scriptContent = "SELECT 1;";

			// When
			var execResult = await postgresContainer.ExecScriptAsync(scriptContent).ConfigureAwait(false);

			// Then
			execResult.ExitCode.Should().Be(0L, execResult.Stderr);
			execResult.Stderr.Should().BeEmpty();
		}

		private async Task InitializeRedisContainer(INetwork network)
		{
			redisContainer = new RedisBuilder("redis")
				.WithReuse(ReuseContainers)
				.WithPortBinding(ReditPort, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ReditPort))
				.WithNetwork(network)
				.Build();
			await redisContainer.StartAsync().ConfigureAwait(false);
		}

		private async Task InitializeGhostfolioContainer(INetwork network)
		{
			await TestcontainersSettings.ExposeHostPortsAsync(postgresContainer.GetMappedPublicPort(PostgresPort)).ConfigureAwait(false);
			await TestcontainersSettings.ExposeHostPortsAsync(redisContainer.GetMappedPublicPort(ReditPort)).ConfigureAwait(false);

			ghostfolioContainer = new ContainerBuilder("ghostfolio/ghostfolio:latest")
				.WithPortBinding(GhostfolioPort, true)
				.WithEnvironment("ACCESS_TOKEN_SALT", Guid.NewGuid().ToString())
				.WithEnvironment("JWT_SECRET_KEY", Guid.NewGuid().ToString())
				.WithEnvironment("REDIS_HOST", TestContainerHostName)
				.WithEnvironment("REDIS_PASSWORD", string.Empty)
				.WithEnvironment("REDIS_PORT", redisContainer.GetMappedPublicPort(ReditPort).ToString())
				.WithEnvironment("DATABASE_URL", $"postgresql://{Username}:{Password}@postgrescontainer/{Database}")
				.WithEnvironment("POSTGRES_DB", Database)
				.WithEnvironment("POSTGRES_PASSWORD", Password)
				.WithEnvironment("POSTGRES_USER", Username)
				.WithEnvironment("REQUEST_TIMEOUT", "60000")
				.WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(GhostfolioPort))
				.WithNetwork(network)
				.Build();

			try
			{
				await ghostfolioContainer.StartAsync().ConfigureAwait(false);
			}
			catch (Exception)
			{
				var (Stdout, Stderr) = await ghostfolioContainer.GetLogsAsync().ConfigureAwait(false);
				throw new Exception(Stderr + Stdout);
			}

			await TestcontainersSettings.ExposeHostPortsAsync(ghostfolioContainer.GetMappedPublicPort(GhostfolioPort)).ConfigureAwait(false);
		}

		private void EnsureContainersAreRunning()
		{
			postgresContainer.State.Should().Be(TestcontainersStates.Running, "the PostgreSQL container should be running.");
			redisContainer.State.Should().Be(TestcontainersStates.Running, "the Redis container should be running.");
			ghostfolioContainer.State.Should().Be(TestcontainersStates.Running, "the Ghostfolio container should be running.");
		}

	}
}
