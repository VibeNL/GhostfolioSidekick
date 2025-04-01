using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Database;
using FluentAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Tests
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
		public async Task CanAddAndRetrieveHolding()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();
				await context.Database.MigrateAsync().ConfigureAwait(false);

				var holding = new Holding
				{
					Id = 1,
					PartialSymbolIdentifiers = new List<PartialSymbolIdentifier>()
				};

				// Act
				context.Holdings.Add(holding);
				await context.SaveChangesAsync();

				// Assert
				var retrievedHolding = await context.Holdings.FindAsync(1);
				retrievedHolding.Should().NotBeNull();
				retrievedHolding.Id.Should().Be(1);
			}
		}

		[Fact]
		public async Task CanAddAndRetrieveActivity()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();
				await context.Database.MigrateAsync().ConfigureAwait(false);

				var activity = new Activity
				{
					Id = 1,
					Date = DateTime.Now,
					AccountId = 1,
					PartialSymbolIdentifiers = new List<PartialSymbolIdentifier>()
				};

				// Act
				context.Activities.Add(activity);
				await context.SaveChangesAsync();

				// Assert
				var retrievedActivity = await context.Activities.FindAsync(1);
				retrievedActivity.Should().NotBeNull();
				retrievedActivity.Id.Should().Be(1);
			}
		}
	}
}
