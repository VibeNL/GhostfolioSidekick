using GhostfolioSidekick.Model;
using Microsoft.Data.Sqlite;

namespace GhostfolioSidekick.UnitTests.Performance
{
	public class PerformanceTaskTests
	{
		private static DbContextOptions<DatabaseContext> CreateOptions(SqliteConnection connection) =>
			new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(connection)
				.Options;

		[Fact]
		public async Task DoWork_RemovesObsoleteHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
			dbContext.HoldingAggregateds.Add(new HoldingAggregated
			{
				Id = 1,
				Symbol = "OLD",
				AssetClass = AssetClass.Equity,
				CalculatedSnapshots = [
					new() {
						AccountId = 1,
						Date = new DateOnly(2024, 1, 1),
						Quantity = 1,
						AverageCostPrice = new Money(Currency.USD, 0),
						CurrentUnitPrice = new Money(Currency.USD, 0),
						TotalInvested = new Money(Currency.USD, 0),
						TotalValue = new Money(Currency.USD, 0)
					}
				]
			});
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync()).ReturnsAsync(() => new DatabaseContext(options));

			var newHolding = new HoldingAggregated { Symbol = "NEW", AssetClass = AssetClass.Equity };
			var calculatorMock = new Mock<IHoldingPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedHoldings()).ReturnsAsync(new List<HoldingAggregated> { newHolding });

			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			Assert.DoesNotContain(verifyContext.HoldingAggregateds, h => h.Symbol == "OLD");
			Assert.Contains(verifyContext.HoldingAggregateds, h => h.Symbol == "NEW");
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_UpdatesExistingHoldingSnapshots()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
			var existingHolding = new HoldingAggregated
			{
				Id = 1,
				Symbol = "A",
				AssetClass = AssetClass.Equity,
				CalculatedSnapshots = [
					new() {
						AccountId = 1,
						Date = new DateOnly(2024, 1, 1),
						Quantity = 1,
						AverageCostPrice = new Money(Currency.USD, 0),
						CurrentUnitPrice = new Money(Currency.USD, 0),
						TotalInvested = new Money(Currency.USD, 0),
						TotalValue = new Money(Currency.USD, 0)
					}
				]
			};
			dbContext.HoldingAggregateds.Add(existingHolding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync()).ReturnsAsync(() => new DatabaseContext(options));

			var newSnapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 2,
				AverageCostPrice = new Money(Currency.USD, 0),
				CurrentUnitPrice = new Money(Currency.USD, 0),
				TotalInvested = new Money(Currency.USD, 0),
				TotalValue = new Money(Currency.USD, 0)
			};
			var holding = new HoldingAggregated { Symbol = "A", AssetClass = AssetClass.Equity, CalculatedSnapshots = [newSnapshot] };
			var calculatorMock = new Mock<IHoldingPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedHoldings()).ReturnsAsync(new List<HoldingAggregated> { holding });

			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
		var updated = await verifyContext.HoldingAggregateds.Include(h => h.CalculatedSnapshots).FirstAsync(h => h.Symbol == "A", TestContext.Current.CancellationToken);
			Assert.Equal(2, updated.CalculatedSnapshots.First().Quantity);
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_DeletesAllHoldings_WhenNoNewHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
			dbContext.HoldingAggregateds.Add(new HoldingAggregated
			{
				Id = 1,
				Symbol = "OLD",
				AssetClass = AssetClass.Equity,
				CalculatedSnapshots = [
					new() {
						AccountId = 1,
						Date = new DateOnly(2024, 1, 1),
						Quantity = 1,
						AverageCostPrice = new Money(Currency.USD, 0),
						CurrentUnitPrice = new Money(Currency.USD, 0),
						TotalInvested = new Money(Currency.USD, 0),
						TotalValue = new Money(Currency.USD, 0)
					}
				]
			});
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync()).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IHoldingPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedHoldings()).ReturnsAsync(new List<HoldingAggregated>());

			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			Assert.Empty(verifyContext.HoldingAggregateds);
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_AddsOnlyNewHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
			// No holdings in DB

			var newHolding = new HoldingAggregated { Symbol = "NEW", AssetClass = AssetClass.Equity };
			var calculatorMock = new Mock<IHoldingPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedHoldings()).ReturnsAsync(new List<HoldingAggregated> { newHolding });

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync()).ReturnsAsync(() => new DatabaseContext(options));

			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var holdings = await verifyContext.HoldingAggregateds.ToListAsync(TestContext.Current.CancellationToken);
			Assert.Single(holdings);
			Assert.Equal("NEW", holdings[0].Symbol);
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}
	}
}


