using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace IntegrationTests
{
	public class IntegrationTestBasic : IAsyncLifetime
	{
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

		public async Task InitializeAsync()
		{
			await StartPostgreSql().ConfigureAwait(false);

			// Create and start the Redis container.
			redisContainer = new RedisBuilder()
				.WithPortBinding(ReditPort, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(ReditPort))
				.Build();
			await redisContainer.StartAsync().ConfigureAwait(false);

			// Create and start the Ghostfolio container.
			await TestcontainersSettings.ExposeHostPortsAsync(postgresContainer.GetMappedPublicPort(PostgresPort)).ConfigureAwait(false);
			await TestcontainersSettings.ExposeHostPortsAsync(redisContainer.GetMappedPublicPort(ReditPort)).ConfigureAwait(false);

			ghostfolioContainer = new ContainerBuilder()
				.WithImage("ghostfolio/ghostfolio:latest")
				.WithPortBinding(3000, true)
				.WithEnvironment("ACCESS_TOKEN_SALT", Guid.NewGuid().ToString())
				.WithEnvironment("JWT_SECRET_KEY", Guid.NewGuid().ToString())
				.WithEnvironment("REDIS_HOST", TestContainerHostName)
				.WithEnvironment("REDIS_PASSWORD", string.Empty)
				.WithEnvironment("REDIS_PORT", redisContainer.GetMappedPublicPort(ReditPort).ToString())
				.WithEnvironment("DATABASE_URL", $"postgresql://{Username}:{Password}@{TestContainerHostName}:{postgresContainer.GetMappedPublicPort(PostgresPort)}/{Database}")
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(GhostfolioPort))
				.Build();

			try
			{
				await ghostfolioContainer.StartAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				var logs = await ghostfolioContainer.GetLogsAsync();
				throw new Exception(logs.Stderr);
			}

			// Initialize HttpClient.
			httpClient = new HttpClient();
		}

		private async Task StartPostgreSql()
		{
			// Create and start the PostgreSQL container.
			postgresContainer = new PostgreSqlBuilder()
				.WithUsername(Username)
				.WithPassword(Password)
				.WithDatabase(Database)
				.WithPortBinding(PostgresPort, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(PostgresPort))
				.Build();
			await postgresContainer.StartAsync().ConfigureAwait(false);

			// Given
			const string scriptContent = "SELECT 1;";

			// When
			var execResult = await postgresContainer.ExecScriptAsync(scriptContent)
				.ConfigureAwait(true);

			// Then
			execResult.ExitCode.Should().Be(0L, execResult.Stderr);
			execResult.Stderr.Should().BeEmpty();
		}

		public async Task DisposeAsync()
		{
			// Dispose HttpClient.
			httpClient.Dispose();

			// Stop and dispose the PostgreSQL container.
			await postgresContainer.StopAsync();
			await postgresContainer.DisposeAsync();

			// Stop and dispose the Redis container.
			await redisContainer.StopAsync();
			await redisContainer.DisposeAsync();

			// Stop and dispose the Ghostfolio container.
			await ghostfolioContainer.StopAsync();
			await ghostfolioContainer.DisposeAsync();
		}

		[Fact]
		public async Task CanSetupGhostfolioDependencies()
		{
			// Ensure the PostgreSQL container is running.
			postgresContainer.State.Should().Be(TestcontainersStates.Running, "the PostgreSQL container should be running.");

			// Ensure the Redis container is running.
			redisContainer.State.Should().Be(TestcontainersStates.Running, "the Redis container should be running.");

			// Ensure the Ghostfolio container is running.
			ghostfolioContainer.State.Should().Be(TestcontainersStates.Running, "the Ghostfolio container should be running.");

			// Example test logic for PostgreSQL, Redis, and Ghostfolio containers.
			// You can add your specific test logic here.

			// Example: Check PostgreSQL connection.
			var postgresConnectionString = $"Host={postgresContainer.Hostname};Port={postgresContainer.GetMappedPublicPort(PostgresPort)};Username=testuser;Password=testpassword;Database=testdb";
			// Add your PostgreSQL connection test logic here.

			// Example: Check Redis connection.
			var redisConnectionString = $"{redisContainer.Hostname}:{redisContainer.GetMappedPublicPort(ReditPort)}";
			// Add your Redis connection test logic here.

			// Example: Check Ghostfolio connection.
			var ghostfolioUri = new UriBuilder(Uri.UriSchemeHttp, ghostfolioContainer.Hostname, ghostfolioContainer.GetMappedPublicPort(GhostfolioPort)).Uri;
			var response = await httpClient.GetAsync(ghostfolioUri);
			response.EnsureSuccessStatusCode();
		}
	}
}
