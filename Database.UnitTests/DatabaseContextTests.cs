using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Database;
using FluentAssertions;
using Testcontainers.MsSql;
using Testcontainers.Container.Abstractions.Hosting;

namespace GhostfolioSidekick.Tests
{
	public class DatabaseContextTests : IAsyncLifetime
	{
		private readonly MsSqlContainer _msSqlContainer;

		public DatabaseContextTests()
		{
			_msSqlContainer = new ContainerBuilder<MsSqlContainer>()
				.ConfigureDatabaseConfiguration("sa", "yourStrong(!)Password", "TestDb")
				.Build();
		}

		public async Task InitializeAsync()
		{
			await _msSqlContainer.StartAsync();
		}

		public async Task DisposeAsync()
		{
			await _msSqlContainer.StopAsync();
		}

		[Fact]
		public async Task CanApplyMigrations()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlServer(_msSqlContainer.GetConnectionString())
				.Options;

			using (var context = new DatabaseContext(options))
			{
				// Act
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Assert
				var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
				pendingMigrations.Should().BeEmpty();

				// Check if table Holding exists
				var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT name FROM sys.tables WHERE name = 'Holdings';");
				tableExists.Should().Be(1);
			}
		}
	}
}
