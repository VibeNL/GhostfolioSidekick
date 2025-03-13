using System.Threading.Tasks;
using Testcontainers.Container.Abstractions.Hosting;
using Testcontainers.Container.Database.PostgreSql;
using Testcontainers.Container.Cache.Redis;
using Xunit;

namespace IntegrationTests
{
    public class IntegrationTest
    {
        [Fact]
        public async Task TestContainersStartUpCorrectly()
        {
            // Start PostgreSQL container
            var postgresContainer = new ContainerBuilder<PostgreSqlContainer>()
                .ConfigureDatabaseConfiguration("postgres", "password", "testdb")
                .Build();
            await postgresContainer.StartAsync();

            // Start Redis container
            var redisContainer = new ContainerBuilder<RedisContainer>()
                .Build();
            await redisContainer.StartAsync();

            // Start Ghostfolio container
            var ghostfolioContainer = new ContainerBuilder<Container>()
                .ConfigureImage("ghostfolio/ghostfolio:latest")
                .Build();
            await ghostfolioContainer.StartAsync();

            // Verify that all containers are running
            Assert.True(postgresContainer.State == ContainerState.Running);
            Assert.True(redisContainer.State == ContainerState.Running);
            Assert.True(ghostfolioContainer.State == ContainerState.Running);

            // Stop containers
            await postgresContainer.StopAsync();
            await redisContainer.StopAsync();
            await ghostfolioContainer.StopAsync();
        }
    }
}
