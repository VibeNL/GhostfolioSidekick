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
		}

		[Fact]
		public async Task CanCreateDbContextWithCalculatedSnapshotConfiguration()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using var context = new DatabaseContext(options);
			await context.Database.OpenConnectionAsync(CancellationToken.None);

			// Act & Assert - Should not throw exception when creating DbContext
			await context.Database.EnsureCreatedAsync(CancellationToken.None);

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
						new Money(Currency.USD, 50m),
						new Money(Currency.USD, 55m),
						new Money(Currency.USD, 5000m),
						new Money(Currency.USD, 5500m)
					)
				}
			};

			context.HoldingAggregateds.Add(holdingAggregated);
			await context.SaveChangesAsync(CancellationToken.None);

		// Verify we can read it back
		var retrieved = await context.HoldingAggregateds
			.Include(x => x.CalculatedSnapshots)
			.FirstOrDefaultAsync(CancellationToken.None);

			retrieved.Should().NotBeNull();
			retrieved!.CalculatedSnapshots.Should().HaveCount(1);

			var snapshot = retrieved.CalculatedSnapshots.First();
			snapshot.Date.Should().Be(DateOnly.FromDateTime(DateTime.Today));
			snapshot.Quantity.Should().Be(100m);
			snapshot.AverageCostPrice.Amount.Should().Be(50m);
			snapshot.CurrentUnitPrice.Amount.Should().Be(55m);
			snapshot.TotalInvested.Amount.Should().Be(5000m);
			snapshot.TotalValue.Amount.Should().Be(5500m);
		}
	}
}
