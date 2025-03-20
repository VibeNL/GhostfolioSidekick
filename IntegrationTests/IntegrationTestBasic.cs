using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace IntegrationTests
{
	public class IntegrationTestBasic : IAsyncLifetime
	{
		private const string Username = "testuser";
		private const string Password = "testpassword";
		private const string Database = "testdb";

		private PostgreSqlContainer postgresContainer = default!;
		private IContainer redisContainer = default!;
		private IContainer ghostfolioContainer = default!;
		private INetwork customNetwork = default!;
		private HttpClient httpClient = default!;

		public async Task InitializeAsync()
		{
			// Create and start the custom network.
			customNetwork = new NetworkBuilder()
				.WithName("custom-network")
				.Build();
			await customNetwork.CreateAsync().ConfigureAwait(false);

			await StartPostgreSql().ConfigureAwait(false);

			// Create and start the Redis container.
			redisContainer = new ContainerBuilder()
				.WithImage("redis:latest")
				.WithPortBinding(6379, true)
				.WithNetwork(customNetwork)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
				.Build();
			await redisContainer.StartAsync().ConfigureAwait(false);

			// Create and start the Ghostfolio container.
			ghostfolioContainer = new ContainerBuilder()
				.WithImage("ghostfolio/ghostfolio:latest")
				.WithPortBinding(3000, true)
				.WithNetwork(customNetwork)
				.WithEnvironment("ACCESS_TOKEN_SALT", Guid.NewGuid().ToString())
				.WithEnvironment("JWT_SECRET_KEY", Guid.NewGuid().ToString())
				.WithEnvironment("REDIS_HOST", redisContainer.Hostname)
				.WithEnvironment("REDIS_PASSWORD", string.Empty)
				.WithEnvironment("REDIS_PORT", redisContainer.GetMappedPublicPort(6379).ToString())
				.WithEnvironment("DATABASE_URL", "postgresql://" + postgresContainer.GetConnectionString())
				// $"postgresql://{postgresContainer.get}:testpassword@{postgresContainer.Hostname}:{postgresContainer.GetMappedPublicPort(5432)}/testdb"
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(3000))
				.Build();
			await ghostfolioContainer.StartAsync().ConfigureAwait(false);

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
				.WithPortBinding(5432, true)
				.WithNetwork(customNetwork)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
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

			// Remove the custom network.
			await customNetwork.DeleteAsync();
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
			var postgresConnectionString = $"Host={postgresContainer.Hostname};Port={postgresContainer.GetMappedPublicPort(5432)};Username=testuser;Password=testpassword;Database=testdb";
			// Add your PostgreSQL connection test logic here.

			// Example: Check Redis connection.
			var redisConnectionString = $"{redisContainer.Hostname}:{redisContainer.GetMappedPublicPort(6379)}";
			// Add your Redis connection test logic here.

			// Example: Check Ghostfolio connection.
			var ghostfolioUri = new UriBuilder(Uri.UriSchemeHttp, ghostfolioContainer.Hostname, ghostfolioContainer.GetMappedPublicPort(3000)).Uri;
			var response = await httpClient.GetAsync(ghostfolioUri);
			response.EnsureSuccessStatusCode();
		}
	}
}
