using AwesomeAssertions;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Tools.Database.UnitTests
{
	public class DatabaseContextTests
	{
		[Fact]
		public async Task CanApplyMigrations()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using var context = new DatabaseContext(options);
			await context.Database.OpenConnectionAsync(CancellationToken.None);

			// Act
			await context.Database.MigrateAsync(CancellationToken.None);

			// Assert
			var pendingMigrations = await context.Database.GetPendingMigrationsAsync(CancellationToken.None);
			pendingMigrations.Should().BeEmpty();

			// Debug: Get all existing tables
			var allTables = await context.Database.SqlQueryRaw<string>(
				"SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;")
				.ToListAsync(CancellationToken.None);
			
			// Output for debugging
			var tablesOutput = string.Join(", ", allTables);
			Console.WriteLine($"Existing tables: {tablesOutput}");

			// Check if Holdings table exists in the list
			allTables.Should().Contain("Holdings");
		}
	}
}
