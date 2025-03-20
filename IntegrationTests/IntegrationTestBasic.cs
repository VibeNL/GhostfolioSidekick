using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests
{
	public class IntegrationTestBasic : IAsyncLifetime
	{
		private IContainer postgresContainer = default!;
		private IContainer redisContainer = default!;
		private IContainer ghostfolioContainer = default!;
		private HttpClient httpClient = default!;

		public async Task InitializeAsync()
		{
			// Create and start the PostgreSQL container.
			postgresContainer = new ContainerBuilder()
				.WithImage("postgres:16")
				.WithPortBinding(5432, true)
				.WithEnvironment("POSTGRES_USER", "testuser")
				.WithEnvironment("POSTGRES_PASSWORD", "testpassword")
				.WithEnvironment("POSTGRES_DB", "testdb")
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
				.Build();
			await postgresContainer.StartAsync().ConfigureAwait(false);

			// Create and start the Redis container.
			redisContainer = new ContainerBuilder()
				.WithImage("redis:latest")
				.WithPortBinding(6379, true)
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
				.Build();
			await redisContainer.StartAsync().ConfigureAwait(false);

			// Create and start the Ghostfolio container.
			ghostfolioContainer = new ContainerBuilder()
				.WithImage("ghostfolio/ghostfolio:latest")
				.WithPortBinding(3000, true)
				.WithEnvironment("DATABASE_URL", $"postgres://testuser:testpassword@{postgresContainer.Hostname}:{postgresContainer.GetMappedPublicPort(5432)}/testdb")
				.WithEnvironment("REDIS_URL", $"redis://{redisContainer.Hostname}:{redisContainer.GetMappedPublicPort(6379)}")
				.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(3000))
				.Build();
			await ghostfolioContainer.StartAsync().ConfigureAwait(false);

			// Initialize HttpClient.
			httpClient = new HttpClient();
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
