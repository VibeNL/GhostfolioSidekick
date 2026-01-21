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
			await context.Database.OpenConnectionAsync();

			// Act
			await context.Database.MigrateAsync();

			// Assert
			var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
			pendingMigrations.Should().BeEmpty();

			// Check if table Holding exists
			var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT name FROM sqlite_master WHERE type='table' AND name='Holdings';");
		}

		[Fact]
		public async Task CanCreateDbContextWithCalculatedSnapshotConfiguration()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using var context = new DatabaseContext(options);
			await context.Database.OpenConnectionAsync();

			// Act & Assert - Should not throw exception when creating DbContext
			await context.Database.EnsureCreatedAsync();

			// Verify that we can add a HoldingAggregated with CalculatedSnapshots
			var holdingAggregated = new HoldingAggregated
			{
				Symbol = "TEST",
				Name = "Test Asset",
				DataSource = "TEST",
				AssetClass = AssetClass.Equity,
				ActivityCount = 1,
				CalculatedSnapshots =
				{
					new CalculatedSnapshot(
						0, 0,
						DateOnly.FromDateTime(DateTime.Today),
						100m,
						Currency.USD,
						50m,
						55m,
						5000m,
						5500m
					)
				}
			};

			context.HoldingAggregateds.Add(holdingAggregated);
			await context.SaveChangesAsync();

			// Verify we can read it back
			var retrieved = await context.HoldingAggregateds
				.Include(x => x.CalculatedSnapshots)
				.FirstOrDefaultAsync();

			retrieved.Should().NotBeNull();
			retrieved!.CalculatedSnapshots.Should().HaveCount(1);

			var snapshot = retrieved.CalculatedSnapshots.First();
			snapshot.Date.Should().Be(DateOnly.FromDateTime(DateTime.Today));
			snapshot.Quantity.Should().Be(100m);
			snapshot.Currency.Should().Be(Currency.USD);
			snapshot.AverageCostPrice.Should().Be(50m);
			snapshot.CurrentUnitPrice.Should().Be(55m);
			snapshot.TotalInvested.Should().Be(5000m);
			snapshot.TotalValue.Should().Be(5500m);
		}
	}
}
