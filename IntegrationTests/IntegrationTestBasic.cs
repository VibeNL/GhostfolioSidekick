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
		private HttpClient httpClient = default!;

		public async Task InitializeAsync()
		{
			// Create a new instance of a PostgreSQL container.
			postgresContainer = new ContainerBuilder()
			  // Set the image for the container to "postgres:16".
			  .WithImage("postgres:16")
			  // Bind port 5432 of the container to a random port on the host.
			  .WithPortBinding(5432, true)
			  // Set environment variables for PostgreSQL.
			  .WithEnvironment("POSTGRES_USER", "testuser")
			  .WithEnvironment("POSTGRES_PASSWORD", "testpassword")
			  .WithEnvironment("POSTGRES_DB", "testdb")
			  // Wait until the PostgreSQL endpoint is available.
			  .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
			  // Build the container configuration.
			  .Build();

			// Create a new instance of a Redis container.
			redisContainer = new ContainerBuilder()
			  // Set the image for the container to "redis:latest".
			  .WithImage("redis:latest")
			  // Bind port 6379 of the container to a random port on the host.
			  .WithPortBinding(6379, true)
			  // Wait until the Redis endpoint is available.
			  .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
			  // Build the container configuration.
			  .Build();

			// Start the PostgreSQL container.
			await postgresContainer.StartAsync()
			  .ConfigureAwait(false);

			// Start the Redis container.
			await redisContainer.StartAsync()
			  .ConfigureAwait(false);

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
		}

		[Fact]
		public async Task CanSetupGhostfolioDependencies()
		{
			// Ensure the PostgreSQL container is running.
			postgresContainer.State.Should().Be(TestcontainersStates.Running, "the PostgreSQL container should be running.");

			// Ensure the Redis container is running.
			redisContainer.State.Should().Be(TestcontainersStates.Running, "the Redis container should be running.");

			// Example test logic for PostgreSQL and Redis containers.
			// You can add your specific test logic here.

			// Example: Check PostgreSQL connection.
			var postgresConnectionString = $"Host={postgresContainer.Hostname};Port={postgresContainer.GetMappedPublicPort(5432)};Username=testuser;Password=testpassword;Database=testdb";
			// Add your PostgreSQL connection test logic here.

			// Example: Check Redis connection.
			var redisConnectionString = $"{redisContainer.Hostname}:{redisContainer.GetMappedPublicPort(6379)}";
			// Add your Redis connection test logic here.
		}
	}
}
