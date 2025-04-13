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
		public async Task ExecutePragma_ShouldPreventSqlInjection()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Act & Assert
				await Assert.ThrowsAsync<ArgumentException>(() => context.ExecutePragma("PRAGMA integrity_check; DROP TABLE Holdings;"));
			}
		}

		[Fact]
		public async Task ExecutePragma_ShouldExecuteValidCommand()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Act
				var result = await context.ExecutePragma("PRAGMA integrity_check;");

				// Assert
				result.Should().Be(0);
			}
		}
	}
}
