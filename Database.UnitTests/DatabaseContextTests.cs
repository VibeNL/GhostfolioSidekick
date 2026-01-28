using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
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

			// Check if table Holding exists
			var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT name FROM sqlite_master WHERE type='table' AND name='Holdings';", CancellationToken.None);

			tableExists.Should().BeGreaterThan(0);
		}
	}
}
