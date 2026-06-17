using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using GhostfolioSidekick.Configuration;
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

		private const string NonAdminUsername = "nonadmin";
		private const string NonAdminPassword = "nonadminpassword";
		private const string NonAdminEmail = "nonadmin@test.com";

		private const int ReditPort = 6379;
		private const int PostgresPort = 5432;
		private const int GhostfolioPort = 3333;

		private PostgreSqlContainer postgresContainer = default!;
		private INetwork network = default!;
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
			string url = new UriBuilder(Uri.UriSchemeHttp, ghostfolioContainer.Hostname, ghostfolioContainer.GetMappedPublicPort(GhostfolioPort)).Uri.ToString();
			_ = authToken.Should().NotBeNull();

			await InitializeSidekick(authToken, url);
		}

		/// <summary>
		/// Integration test: Non-admin user sync with AllowAdminCalls=false.
		/// Spins up own containers, creates non-admin user, verifies sync.
		/// </summary>
		[Fact(Timeout = 900000)]
		public async Task GhostfolioNonAdminUserSyncTest()
		{
			await using var network = new NetworkBuilder()
				.WithCleanUp(true)
				.WithName($"ghostfolio-nonadmin-{Guid.NewGuid():N}")
				.Build();

			await using var postgres = new PostgreSqlBuilder("postgres:16")
				.WithUsername(Username)
				.WithPassword(Password)
				.WithDatabase(Database)
				.WithNetwork(network)
				.WithNetworkAliases("postgrescontainer")
				.WithImagePullPolicy(PullPolicy.Always)
				.Build();

			await using var redis = new RedisBuilder("redis")
				.WithNetwork(network)
				.WithImagePullPolicy(PullPolicy.Always)
				.Build();

			IContainer ghostfolio = null!;

			try
			{
				await postgres.StartAsync(TestContext.Current.CancellationToken);
					await redis.StartAsync(TestContext.Current.CancellationToken);

					await TestcontainersSettings.ExposeHostPortsAsync(postgres.GetMappedPublicPort(PostgresPort), TestContext.Current.CancellationToken);
					await TestcontainersSettings.ExposeHostPortsAsync(redis.GetMappedPublicPort(ReditPort), TestContext.Current.CancellationToken);

				ghostfolio = new ContainerBuilder("ghostfolio/ghostfolio:latest")
					.WithPortBinding(GhostfolioPort, true)
					.WithEnvironment("ACCESS_TOKEN_SALT", Guid.NewGuid().ToString())
					.WithEnvironment("JWT_SECRET_KEY", Guid.NewGuid().ToString())
					.WithEnvironment("IS_AUTH_ENABLED", "true")
					.WithEnvironment("REDIS_HOST", TestContainerHostName)
					.WithEnvironment("REDIS_PASSWORD", string.Empty)
					.WithEnvironment("REDIS_PORT", redis.GetMappedPublicPort(ReditPort).ToString())
					.WithEnvironment("DATABASE_URL", $"postgresql://{Username}:{Password}@postgrescontainer/{Database}")
					.WithEnvironment("POSTGRES_DB", Database)
					.WithEnvironment("POSTGRES_PASSWORD", Password)
					.WithEnvironment("POSTGRES_USER", Username)
					.WithEnvironment("REQUEST_TIMEOUT", "60000")
					.WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(GhostfolioPort))
					.WithNetwork(network)
					.WithImagePullPolicy(PullPolicy.Always)
					.Build();

				await ghostfolio.StartAsync(TestContext.Current.CancellationToken);

				await TestcontainersSettings.ExposeHostPortsAsync(ghostfolio.GetMappedPublicPort(GhostfolioPort), TestContext.Current.CancellationToken);

				var url = new UriBuilder(Uri.UriSchemeHttp, ghostfolio.Hostname, ghostfolio.GetMappedPublicPort(GhostfolioPort)).Uri.ToString();

				// Create admin user (first user).
				var adminRequest = new HttpRequestMessage(HttpMethod.Post, $"{url}api/v1/user")
				{
					Content = new StringContent("{}")
				};
				var adminResponse = await httpClient.SendAsync(adminRequest, TestContext.Current.CancellationToken);
				_ = adminResponse.EnsureSuccessStatusCode();
				var adminAuth = await adminResponse.Content.ReadFromJsonAsync<AuthData>(cancellationToken: TestContext.Current.CancellationToken);
				_ = adminAuth.Should().NotBeNull();

				// Create non-admin user authenticated as admin.
				var nonAdminBody = System.Text.Json.JsonSerializer.Serialize(new
				{
					username = NonAdminUsername,
					otp2faSecret = (string?)null,
					oneTimePassword = (string?)null,
					password = NonAdminPassword,
					email = NonAdminEmail
				});
				var nonAdminRequest = new HttpRequestMessage(HttpMethod.Post, $"{url}api/v1/user")
				{
					Content = new StringContent(nonAdminBody)
				};
				nonAdminRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
				var nonAdminResponse = await httpClient.SendAsync(nonAdminRequest, TestContext.Current.CancellationToken);
				_ = nonAdminResponse.EnsureSuccessStatusCode();
				var nonAdminAuth = await nonAdminResponse.Content.ReadFromJsonAsync<AuthData>(cancellationToken: TestContext.Current.CancellationToken);
				_ = nonAdminAuth.Should().NotBeNull();
				nonAdminAuth!.AccessToken.Should().NotBeNullOrEmpty();

				// Initialize sidekick with non-admin token.
				var dbPath = Path.Combine(Path.GetTempPath(), $"ghostfolio_sidekick_non_admin_{Guid.NewGuid():N}.db");
				if (System.IO.File.Exists(dbPath))
				{
					System.IO.File.Delete(dbPath);
				}

				Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", url);
				Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", nonAdminAuth.AccessToken);
				Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", "./Files/");
				Environment.SetEnvironmentVariable("CONFIGURATIONFILE_PATH", "./Files/config.json");
				Environment.SetEnvironmentVariable("DATABASE_PATH", dbPath);
					Environment.SetEnvironmentVariable("TROTTLE_WAITINSECONDS", "0");

				var testLogger = new TestLogger("Service SyncAccountsWithGhostfolioTask has executed.");
				var testHost = Program
					.CreateHostBuilder()
					.ConfigureServices((hostContext, services) =>
					{
						_ = services.AddSingleton<ILogger<TimedHostedService>>(testLogger);
					})
					.Build();

				// Override AllowAdminCalls to false.
				var settings = testHost.Services.GetRequiredService<IApplicationSettings>();
				((ApplicationSettings)settings).AllowAdminCalls = false;

				var host = testHost.Services.GetService<IHostedService>();
				var apiWrapper = testHost.Services.GetRequiredService<IApiWrapper>();

				await host!.StartAsync(CancellationToken.None);

				// Wait max 3 minutes for sync to complete.
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				while (!testLogger.IsTriggered && stopwatch.Elapsed.TotalMinutes < 5)
				{
					await Task.Delay(1000, TestContext.Current.CancellationToken);
				}

				// Dump logs on failure for debugging.
				if (!testLogger.IsTriggered)
				{
					var lastMessages = testLogger.Messages.TakeLast(50).ToArray();
					throw new Exception($"Sync did not complete. Last {lastMessages.Length} log messages: {string.Join("\n", lastMessages)}");
				}

				_ = testLogger.IsTriggered.Should().BeTrue(because: "non-admin sync must complete without admin endpoint errors");

				// Verify accounts exist (AccountExistsAsync uses api/v1/account which works for non-admin).
				_ = (await apiWrapper.AccountExistsAsync("TestAccount1")).Should().BeTrue(because: "non-admin sync should create accounts in Ghostfolio");
				_ = (await apiWrapper.AccountExistsAsync("TestAccount2")).Should().BeTrue(because: "non-admin sync should create accounts in Ghostfolio");
			}
			finally
			{
				if (ghostfolio != null)
				{
					await ghostfolio.StopAsync(TestContext.Current.CancellationToken);
					await ghostfolio.DisposeAsync();
				}
			}
		}

		private const string NetworkName = "ghostfolio-network";

		public async ValueTask InitializeAsync()
		{
			network = new NetworkBuilder()
				.WithCleanUp(true)
				.WithName(NetworkName)
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
			var response = await httpClient.GetAsync(ghostfolioUri, TestContext.Current.CancellationToken).ConfigureAwait(false);
			_ = response.EnsureSuccessStatusCode();

			// Create admin user (first user is admin).
			response = await httpClient.PostAsync($"{ghostfolioUri}api/v1/user", new StringContent("{}")).ConfigureAwait(false);
			_ = response.EnsureSuccessStatusCode();
			authToken = await response.Content.ReadFromJsonAsync<AuthData>().ConfigureAwait(false);
		}

		public async ValueTask DisposeAsync()
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

			// Dispose the network.
			await DeleteNetworkIfExistsAsync(network).ConfigureAwait(false);
		}

		private async Task InitializeSidekick(AuthData authToken, string url)
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", url);
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", authToken.AccessToken);
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", "./Files/");
			Environment.SetEnvironmentVariable("CONFIGURATIONFILE_PATH", "./Files/config.json");

			// Clean up existing database file if present
			string dbPath = "./Files/ghostfolio.db";
			if (System.IO.File.Exists(dbPath))
			{
				System.IO.File.Delete(dbPath);
			}

			var testLogger = new TestLogger("Service SyncActivitiesWithGhostfolioTask has executed.");
			var testHost = Program
				.CreateHostBuilder()
				.ConfigureServices((hostContext, services) =>
				{
					_ = services.AddSingleton<ILogger<TimedHostedService>>(testLogger);
				})
				.Build();

			var host = testHost.Services.GetService<IHostedService>();
			var c = new CancellationToken();

			var apiWrapper = testHost.Services.GetRequiredService<IApiWrapper>();

			// Act
			await host!.StartAsync(c);

			while (!testLogger.IsTriggered)
			{
				await Task.Delay(1000, TestContext.Current.CancellationToken);
			}

			// Assert
			await VerifyInstance(apiWrapper);
		}

		private async Task VerifyInstance(IApiWrapper apiWrapper)
		{
			foreach (var item in AccountsWithExpectedNumbers)
			{
				var account = await apiWrapper.GetAccountByName(item.Key);
				_ = account.Should().NotBeNull();

				var activities = await apiWrapper.GetActivitiesByAccount(account!);
				_ = activities.Should().HaveCount(item.Value);
			}

			await VerifyFeesAreProcessed(apiWrapper);
		}

		private static async Task VerifyFeesAreProcessed(IApiWrapper apiWrapper)
		{
			var account = await apiWrapper.GetAccountByName("TestAccount1");
			_ = account.Should().NotBeNull();

			var activities = await apiWrapper.GetActivitiesByAccount(account!);
			var buyActivities = activities.OfType<Model.Activities.Types.BuyActivity>().ToList();

			_ = buyActivities.Should().NotBeEmpty("TestAccount1 should have Buy activities with fees");
			_ = buyActivities.Should().AllSatisfy(a =>
			{
				_ = a.Fees.Should().HaveCount(1, $"buy activity {a.TransactionId} should have exactly one fee");
				_ = a.Fees.First().Amount.Should().Be(0.02m, $"buy activity {a.TransactionId} should have a fee of 0.02");
			});
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
				.WithImagePullPolicy(PullPolicy.Always)
				.Build();
			await postgresContainer.StartAsync().ConfigureAwait(false);

			// Given
			const string scriptContent = "SELECT 1;";

			// When
			var execResult = await postgresContainer.ExecScriptAsync(scriptContent).ConfigureAwait(false);

			// Then
			_ = execResult.ExitCode.Should().Be(0L, execResult.Stderr);
			_ = execResult.Stderr.Should().BeEmpty();
		}

		private async Task InitializeRedisContainer(INetwork network)
		{
			redisContainer = new RedisBuilder("redis")
				.WithReuse(ReuseContainers)
				.WithPortBinding(ReditPort, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ReditPort))
				.WithNetwork(network)
				.WithImagePullPolicy(PullPolicy.Always)
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
				.WithEnvironment("IS_AUTH_ENABLED", "true")
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
				.WithImagePullPolicy(PullPolicy.Always)
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

		private static async Task DeleteNetworkIfExistsAsync(INetwork? network)
		{
			if (network != null)
			{
				await network.DisposeAsync();
			}
		}

		private void EnsureContainersAreRunning()
		{
			_ = postgresContainer.State.Should().Be(TestcontainersStates.Running, "the PostgreSQL container should be running.");
			_ = redisContainer.State.Should().Be(TestcontainersStates.Running, "the Redis container should be running.");
			_ = ghostfolioContainer.State.Should().Be(TestcontainersStates.Running, "the Ghostfolio container should be running.");
		}

	}
}


