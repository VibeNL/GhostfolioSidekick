using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace IntegrationTests
{
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

		[Fact]
		public async Task CanSetupGhostfolioDependencies()
		{
			// url ghostfolio for debugging:
			var url = new UriBuilder(Uri.UriSchemeHttp, ghostfolioContainer.Hostname, ghostfolioContainer.GetMappedPublicPort(GhostfolioPort)).Uri.ToString();
			authToken.Should().NotBeNull();


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

		private async Task InitializePostgresContainer(INetwork network)
		{
			postgresContainer = new PostgreSqlBuilder()
				.WithReuse(ReuseContainers)
				.WithUsername(Username)
				.WithPassword(Password)
				.WithDatabase(Database)
				.WithPortBinding(PostgresPort, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(PostgresPort))
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
			redisContainer = new RedisBuilder()
				.WithReuse(ReuseContainers)
				.WithPortBinding(ReditPort, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(ReditPort))
				.WithNetwork(network)
				.Build();
			await redisContainer.StartAsync().ConfigureAwait(false);
		}

		private async Task InitializeGhostfolioContainer(INetwork network)
		{
			await TestcontainersSettings.ExposeHostPortsAsync(postgresContainer.GetMappedPublicPort(PostgresPort)).ConfigureAwait(false);
			await TestcontainersSettings.ExposeHostPortsAsync(redisContainer.GetMappedPublicPort(ReditPort)).ConfigureAwait(false);

			ghostfolioContainer = new ContainerBuilder()
				.WithImage("ghostfolio/ghostfolio:latest")
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
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(GhostfolioPort))
				.WithNetwork(network)
				.Build();

			try
			{
				await ghostfolioContainer.StartAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				var logs = await ghostfolioContainer.GetLogsAsync().ConfigureAwait(false);
				throw new Exception(logs.Stderr + logs.Stdout);
			}

			await TestcontainersSettings.ExposeHostPortsAsync(ghostfolioContainer.GetMappedPublicPort(GhostfolioPort)).ConfigureAwait(false);
		}

		private void EnsureContainersAreRunning()
		{
			postgresContainer.State.Should().Be(TestcontainersStates.Running, "the PostgreSQL container should be running.");
			redisContainer.State.Should().Be(TestcontainersStates.Running, "the Redis container should be running.");
			ghostfolioContainer.State.Should().Be(TestcontainersStates.Running, "the Ghostfolio container should be running.");
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
	}
}
