using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Database;
using FluentAssertions;

namespace GhostfolioSidekick.Tools.Database.UnitTests
{
	public class DatabaseContextTests
	{
		public DatabaseContextTests()
		{

		}

		[Fact]
		public async Task CanApplyMigrations()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();

				// Act
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Assert
				var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
				pendingMigrations.Should().BeEmpty();

				// Check if table Holding exists
				var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT name FROM sqlite_master WHERE type='table' AND name='Holdings';");
			}
		}

		[Fact]
		public async Task CanApplyMigrationsWithSqlServer()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDatabase;Trusted_Connection=True;")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();

				// Act
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Assert
				var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
				pendingMigrations.Should().BeEmpty();

				// Check if table Holding exists
				var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT name FROM sys.tables WHERE name = 'Holdings';");
			}
		}

		[Fact]
		public async Task CanApplyMigrationsWithPostgreSql()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseNpgsql("Host=localhost;Database=testdb;Username=testuser;Password=testpassword")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();

				// Act
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Assert
				var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
				pendingMigrations.Should().BeEmpty();

				// Check if table Holding exists
				var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT table_name FROM information_schema.tables WHERE table_name = 'Holdings';");
			}
		}
	}
}
