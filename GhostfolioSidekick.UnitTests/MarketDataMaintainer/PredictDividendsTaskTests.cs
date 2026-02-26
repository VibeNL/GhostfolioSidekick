using AwesomeAssertions;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class PredictDividendsTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly PredictDividendsTask _task;

		public PredictDividendsTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_task = new PredictDividendsTask(_mockDbContextFactory.Object);
		}

		[Fact]
		public void Priority_ShouldReturnPredictDividends()
		{
			_task.Priority.Should().Be(TaskPriority.PredictDividends);
		}

		[Fact]
		public void ExecutionFrequency_ShouldReturnOneDay()
		{
			_task.ExecutionFrequency.Should().Be(TimeSpan.FromDays(1));
		}

		[Fact]
		public void ExceptionsAreFatal_ShouldReturnFalse()
		{
			_task.ExceptionsAreFatal.Should().BeFalse();
		}

		[Fact]
		public void Name_ShouldReturnCorrectName()
		{
			_task.Name.Should().Be("Predict Dividends Task");
		}

		[Fact]
		public async Task DoWork_WhenNoCalculatedSnapshots_ShouldReturnEarlyWithoutSavingChanges()
		{
			// Arrange
			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_mockDbContextFactory
				.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext.Object);

			// Act
			await _task.DoWork(new Mock<ILogger>().Object);

			// Assert
			mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_WhenHoldingHasZeroQuantity_ShouldLogZeroPredictionsForZeroSymbols()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "AAPL", today.AddDays(-1), quantity: 0);
			var loggerMock = SetupDbContext([snapshot], [holding], []);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: quantity = 0 excludes the holding from the holdings map
			VerifyLogContains(loggerMock, "0 predictions for 0 symbols");
		}

		[Fact]
		public async Task DoWork_WhenHoldingHasNoSymbolProfile_ShouldLogZeroPredictionsForZeroSymbols()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var holding = new Holding { Id = 1, SymbolProfiles = [] };
			var snapshot = new CalculatedSnapshot { HoldingId = 1, Date = today.AddDays(-1), Quantity = 10 };
			var loggerMock = SetupDbContext([snapshot], [holding], []);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: no symbol profile → FirstOrDefault returns null → excluded from holdings map
			VerifyLogContains(loggerMock, "0 predictions for 0 symbols");
		}

		[Fact]
		public async Task DoWork_WhenSymbolHasOnlyOneHistoricalDividend_ShouldLogZeroPredictions()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "AAPL", today.AddDays(-1), quantity: 10);
			var dividends = new List<Dividend> { BuildPaidDividend("AAPL", today.AddDays(-90), 1.0m) };
			var loggerMock = SetupDbContext([snapshot], [holding], dividends);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: history.Count = 1 < 2 → symbol skipped, no intervals to compute
			VerifyLogContains(loggerMock, "0 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenDividendIntervalIsBelowMinimumThreshold_ShouldLogZeroPredictions()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "AAPL", today.AddDays(-1), quantity: 10);

			// Two dividends 7 days apart → median interval = 7, which is below the 14-day minimum
			var dividends = new List<Dividend>
			{
				BuildPaidDividend("AAPL", today.AddDays(-14), 1.0m),
				BuildPaidDividend("AAPL", today.AddDays(-7), 1.0m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], dividends);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: intervalDays = 7 < 14 → symbol skipped
			VerifyLogContains(loggerMock, "0 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenSymbolHasValidAnnualDividendHistory_ShouldAddOnePrediction()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "AAPL", today.AddDays(-1), quantity: 10);

			// Two annual dividends: last ~30 days ago, previous ~395 days before that.
			// Median interval = 365 days → one projection at today+335, within the 12-month horizon.
			var dividends = new List<Dividend>
			{
				BuildPaidDividend("AAPL", today.AddDays(-395), 1.50m),
				BuildPaidDividend("AAPL", today.AddDays(-30), 1.60m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], dividends);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert
			VerifyLogContains(loggerMock, "1 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenProjectedDateIsCoveredByConfirmedUpcoming_ShouldSkipThatPrediction()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "AAPL", today.AddDays(-1), quantity: 10);

			var dividends = new List<Dividend>
			{
				BuildPaidDividend("AAPL", today.AddDays(-395), 1.50m),
				BuildPaidDividend("AAPL", today.AddDays(-30), 1.60m),
				// Declared (confirmed, non-predicted) dividend exactly at the single projected date
				new Dividend
				{
					PaymentDate = today.AddDays(335),
					ExDividendDate = today.AddDays(321),
					DividendType = DividendType.Cash,
					DividendState = DividendState.Declared,
					Amount = new Money(Currency.USD, 1.60m),
					SymbolProfileSymbol = "AAPL",
					SymbolProfileDataSource = "YAHOO"
				}
			};
			var loggerMock = SetupDbContext([snapshot], [holding], dividends);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: the only projected date is covered by a confirmed upcoming → 0 new predictions
			VerifyLogContains(loggerMock, "0 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenExistingPredictionsExist_ShouldReplaceThemWithFreshlyComputedOnes()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "AAPL", today.AddDays(-1), quantity: 10);

			var dividends = new List<Dividend>
			{
				// Stale prediction from a previous run at a date that differs from the new projection
				new Dividend
				{
					PaymentDate = today.AddDays(200),
					ExDividendDate = today.AddDays(186),
					DividendType = DividendType.Cash,
					DividendState = DividendState.Predicted,
					Amount = new Money(Currency.USD, 1.00m),
					SymbolProfileSymbol = "AAPL",
					SymbolProfileDataSource = "YAHOO"
				},
				BuildPaidDividend("AAPL", today.AddDays(-395), 1.50m),
				BuildPaidDividend("AAPL", today.AddDays(-30), 1.60m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], dividends);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: old prediction removed, 1 freshly computed prediction added
			VerifyLogContains(loggerMock, "1 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenMultipleSymbolsHeld_ShouldAddPredictionsForEachSymbol()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holdingAapl, snapshotAapl) = BuildHolding(1, "AAPL", today.AddDays(-1), quantity: 10);
			var (holdingMsft, snapshotMsft) = BuildHolding(2, "MSFT", today.AddDays(-1), quantity: 5);

			var dividends = new List<Dividend>
			{
				BuildPaidDividend("AAPL", today.AddDays(-395), 1.50m),
				BuildPaidDividend("AAPL", today.AddDays(-30), 1.60m),
				BuildPaidDividend("MSFT", today.AddDays(-400), 0.75m),
				BuildPaidDividend("MSFT", today.AddDays(-35), 0.80m),
			};
			var loggerMock = SetupDbContext([snapshotAapl, snapshotMsft], [holdingAapl, holdingMsft], dividends);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: 1 annual projection per symbol → 2 total
			VerifyLogContains(loggerMock, "2 predictions for 2 symbols");
		}

		private Mock<ILogger> SetupDbContext(
			List<CalculatedSnapshot> snapshots,
			List<Holding> holdings,
			List<Dividend> dividends)
		{
			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(snapshots);
			mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext.Setup(db => db.Dividends).ReturnsDbSet(dividends);
			mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

			_mockDbContextFactory
				.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext.Object);

			return new Mock<ILogger>();
		}

		private static void VerifyLogContains(Mock<ILogger> loggerMock, string expected)
		{
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expected)),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		private static (Holding Holding, CalculatedSnapshot Snapshot) BuildHolding(
			int id, string symbol, DateOnly snapshotDate, decimal quantity)
		{
			var symbolProfile = new SymbolProfile { Symbol = symbol, DataSource = "YAHOO" };
			var holding = new Holding { Id = id, SymbolProfiles = [symbolProfile] };
			var snapshot = new CalculatedSnapshot { HoldingId = id, Date = snapshotDate, Quantity = quantity };
			return (holding, snapshot);
		}

		private static Dividend BuildPaidDividend(string symbol, DateOnly paymentDate, decimal amount)
		{
			return new Dividend
			{
				PaymentDate = paymentDate,
				ExDividendDate = paymentDate.AddDays(-14),
				DividendType = DividendType.Cash,
				DividendState = DividendState.Paid,
				Amount = new Money(Currency.USD, amount),
				SymbolProfileSymbol = symbol,
				SymbolProfileDataSource = "YAHOO"
			};
		}
	}
}
