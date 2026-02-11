using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Performance;
using GhostfolioSidekick.PerformanceCalculations;
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
		public async Task DoWork_CalculatesPerformanceForAllHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding { Id = 1 };
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var newSnapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				Currency = Currency.USD,
				AverageCostPrice = 100,
				CurrentUnitPrice = 120,
				TotalInvested = 1000,
				TotalValue = 1200
			};

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>()))
				.ReturnsAsync(new List<CalculatedSnapshot> { newSnapshot });

			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var updatedHolding = await verifyContext.Holdings
				.Include(h => h.CalculatedSnapshots)
				.FirstAsync(h => h.Id == 1, TestContext.Current.CancellationToken);

			Assert.Single(updatedHolding.CalculatedSnapshots);
			Assert.Equal(10, updatedHolding.CalculatedSnapshots.First().Quantity);
			Assert.Equal(1200, updatedHolding.CalculatedSnapshots.First().TotalValue);

			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_UpdatesExistingSnapshots()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding
			{
				Id = 1,
				CalculatedSnapshots =
				[
					new()
					{
						AccountId = 1,
						Date = new DateOnly(2024, 1, 1),
						Quantity = 5,
						Currency = Currency.USD,
						AverageCostPrice = 100,
						CurrentUnitPrice = 110,
						TotalInvested = 500,
						TotalValue = 550,
						HoldingId = 1
					}
				]
			};
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var updatedSnapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				Currency = Currency.USD,
				AverageCostPrice = 100,
				CurrentUnitPrice = 120,
				TotalInvested = 1000,
				TotalValue = 1200
			};

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>()))
				.ReturnsAsync(new List<CalculatedSnapshot> { updatedSnapshot });

			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var updatedHolding = await verifyContext.Holdings
				.Include(h => h.CalculatedSnapshots)
				.FirstAsync(h => h.Id == 1, TestContext.Current.CancellationToken);

			Assert.Single(updatedHolding.CalculatedSnapshots);
			Assert.Equal(10, updatedHolding.CalculatedSnapshots.First().Quantity);
			Assert.Equal(1200, updatedHolding.CalculatedSnapshots.First().TotalValue);

			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_RemovesObsoleteSnapshots()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding
			{
				Id = 1,
				CalculatedSnapshots =
				[
					new()
					{
						AccountId = 1,
						Date = new DateOnly(2024, 1, 1),
						Quantity = 5,
						Currency = Currency.USD,
						AverageCostPrice = 100,
						CurrentUnitPrice = 110,
						TotalInvested = 500,
						TotalValue = 550,
						HoldingId = 1
					},
					new()
					{
						AccountId = 1,
						Date = new DateOnly(2024, 1, 2),
						Quantity = 6,
						Currency = Currency.USD,
						AverageCostPrice = 100,
						CurrentUnitPrice = 115,
						TotalInvested = 600,
						TotalValue = 690,
						HoldingId = 1
					}
				]
			};
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			// Calculator only returns snapshot for 2024-01-01, so 2024-01-02 should be removed
			var newSnapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				Currency = Currency.USD,
				AverageCostPrice = 100,
				CurrentUnitPrice = 120,
				TotalInvested = 1000,
				TotalValue = 1200
			};

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>()))
				.ReturnsAsync(new List<CalculatedSnapshot> { newSnapshot });

			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var updatedHolding = await verifyContext.Holdings
				.Include(h => h.CalculatedSnapshots)
				.FirstAsync(h => h.Id == 1, TestContext.Current.CancellationToken);

			Assert.Single(updatedHolding.CalculatedSnapshots);
			Assert.Equal(new DateOnly(2024, 1, 1), updatedHolding.CalculatedSnapshots.First().Date);

			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_HandlesNoHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
			// No holdings in DB

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IPerformanceCalculator>();
			var loggerMock = new Mock<ILogger>();
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert - should complete without error
			calculatorMock.Verify(x => x.GetCalculatedSnapshots(It.IsAny<Holding>()), Times.Never);
			await dbContext.DisposeAsync();
		}
	}
}

