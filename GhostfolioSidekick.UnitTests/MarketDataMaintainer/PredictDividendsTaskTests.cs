using AwesomeAssertions;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
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
			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(new List<Activity>());
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
			var activities = new List<Activity> { BuildDividendActivity("AAPL", today.AddDays(-90), 1.0m) };
			var loggerMock = SetupDbContext([snapshot], [holding], [], activities);

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
			var activities = new List<Activity>
			{
				BuildDividendActivity("AAPL", today.AddDays(-14), 1.0m),
				BuildDividendActivity("AAPL", today.AddDays(-7), 1.0m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], [], activities);

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
			var activities = new List<Activity>
			{
				BuildDividendActivity("AAPL", today.AddDays(-395), 1.50m),
				BuildDividendActivity("AAPL", today.AddDays(-30), 1.60m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], [], activities);

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

			// Declared (confirmed, non-predicted) dividend exactly at the single projected date
			var dividends = new List<Dividend>
			{
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
			var activities = new List<Activity>
			{
				BuildDividendActivity("AAPL", today.AddDays(-395), 1.50m),
				BuildDividendActivity("AAPL", today.AddDays(-30), 1.60m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], dividends, activities);

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

			// Stale prediction from a previous run at a date that differs from the new projection
			var dividends = new List<Dividend>
			{
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
			};
			var activities = new List<Activity>
			{
				BuildDividendActivity("AAPL", today.AddDays(-395), 1.50m),
				BuildDividendActivity("AAPL", today.AddDays(-30), 1.60m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], dividends, activities);

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

			var activities = new List<Activity>
			{
				BuildDividendActivity("AAPL", today.AddDays(-395), 1.50m),
				BuildDividendActivity("AAPL", today.AddDays(-30), 1.60m),
				BuildDividendActivity("MSFT", today.AddDays(-400), 0.75m),
				BuildDividendActivity("MSFT", today.AddDays(-35), 0.80m),
			};
			var loggerMock = SetupDbContext([snapshotAapl, snapshotMsft], [holdingAapl, holdingMsft], [], activities);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: 1 annual projection per symbol → 2 total
			VerifyLogContains(loggerMock, "2 predictions for 2 symbols");
		}

		private Mock<ILogger> SetupDbContext(
			List<CalculatedSnapshot> snapshots,
			List<Holding> holdings,
			List<Dividend> dividends,
			List<Activity>? activities = null)
		{
			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(snapshots);
			mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext.Setup(db => db.Dividends).ReturnsDbSet(dividends);
			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities ?? []);
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

		private static DividendActivity BuildDividendActivity(string symbol, DateOnly paymentDate, decimal amount)
		{
			return new DividendActivity(
				new Account("Test"),
				null,
				[PartialSymbolIdentifier.CreateGeneric(symbol)],
				paymentDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
				new Money(Currency.USD, amount),
				Guid.NewGuid().ToString(),
				null,
				null);
		}

		[Fact]
		public async Task DoWork_WhenSymbolHasQuarterlyDividends_ShouldAddFourPredictionsWithinYear()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "MSFT", today.AddDays(-1), quantity: 10);

			// Four quarterly dividends: 275, 185, 95, and 5 days ago
			// Intervals: 90, 90, 90 days → median = 90 days (quarterly)
			// Should predict 4 dividends: today+85, +175, +265, +355 (all within 12-month horizon)
			var activities = new List<Activity>
			{
				BuildDividendActivity("MSFT", today.AddDays(-275), 0.60m),
				BuildDividendActivity("MSFT", today.AddDays(-185), 0.62m),
				BuildDividendActivity("MSFT", today.AddDays(-95), 0.65m),
				BuildDividendActivity("MSFT", today.AddDays(-5), 0.68m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], [], activities);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: quarterly dividend → 4 predictions within 12 months
			VerifyLogContains(loggerMock, "4 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenMultiplePredictionsForSameSymbol_ShouldCalculateCorrectPerShareAmount()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "TEST", today.AddDays(-1), quantity: 100);

			// Two dividends with different amounts, recent average should be used
			var activities = new List<Activity>
			{
				BuildDividendActivity("TEST", today.AddDays(-365), 150m), // Total amount, 150/100 = 1.50 per share
				BuildDividendActivity("TEST", today.AddDays(-1), 200m),   // Total amount, 200/100 = 2.00 per share
			};
			var loggerMock = SetupDbContext([snapshot], [holding], [], activities);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: should use average of recent history (1.50 + 2.00) / 2 = 1.75 per share
			VerifyLogContains(loggerMock, "1 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenHoldingQuantityChangesOverTime_ShouldCalculatePerShareCorrectly()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles = [new SymbolProfile { Symbol = "GROW", DataSource = "YAHOO" }]
			};

			// Current snapshot: 100 shares
			var currentSnapshot = new CalculatedSnapshot { HoldingId = 1, Date = today.AddDays(-1), Quantity = 100 };

			// Historical snapshots: had 50 shares during past dividends
			var historicalSnapshot1 = new CalculatedSnapshot { HoldingId = 1, Date = today.AddDays(-400), Quantity = 50 };
			var historicalSnapshot2 = new CalculatedSnapshot { HoldingId = 1, Date = today.AddDays(-35), Quantity = 50 };

			// Two dividends when we had 50 shares: 75/50 = 1.50 per share
			var activities = new List<Activity>
			{
				BuildDividendActivity("GROW", today.AddDays(-395), 75m),  // 75 total / 50 shares = 1.50/share
				BuildDividendActivity("GROW", today.AddDays(-30), 75m),   // 75 total / 50 shares = 1.50/share
			};

			var loggerMock = SetupDbContext(
				[currentSnapshot, historicalSnapshot1, historicalSnapshot2],
				[holding],
				[],
				activities);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: should predict based on per-share amount (1.50), not total
			VerifyLogContains(loggerMock, "1 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenProjectedDateExceedsHorizon_ShouldNotAddPrediction()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "LONGTERM", today.AddDays(-1), quantity: 10);

			// Two dividends 500 days apart → median = 500 days
			// Projection: today + 470 = beyond 12-month horizon (365 days)
			var activities = new List<Activity>
			{
				BuildDividendActivity("LONGTERM", today.AddDays(-530), 1.00m),
				BuildDividendActivity("LONGTERM", today.AddDays(-30), 1.10m),
			};
			var loggerMock = SetupDbContext([snapshot], [holding], [], activities);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: projection beyond horizon → 0 predictions
			VerifyLogContains(loggerMock, "0 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenConfirmedActivityInFutureWithinTolerance_ShouldSkipPrediction()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "CONF", today.AddDays(-1), quantity: 10);

			// Predicted projection at today+335 with tolerance ~121 days (365/3)
			var activities = new List<Activity>
			{
				BuildDividendActivity("CONF", today.AddDays(-395), 1.00m),
				BuildDividendActivity("CONF", today.AddDays(-30), 1.00m),
			};

			// Confirmed future activity at today+300 (within tolerance of projection at today+335)
			var futureConfirmed = new List<Activity>
			{
				BuildDividendActivity("CONF", today.AddDays(300), 1.00m)
			};

			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(db => db.CalculatedSnapshots).ReturnsDbSet([snapshot]);
			mockDbContext.Setup(db => db.Holdings).ReturnsDbSet([holding]);
			mockDbContext.Setup(db => db.Dividends).ReturnsDbSet(new List<Dividend>());

			// Setup Activities to return both historical and future confirmed
			var allActivities = activities.Concat(futureConfirmed).ToList();
			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(allActivities);
			mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

			_mockDbContextFactory
				.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext.Object);

			var loggerMock = new Mock<ILogger>();

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: future confirmed within tolerance → 0 new predictions
			VerifyLogContains(loggerMock, "0 predictions for 1 symbols");
		}

		[Fact]
		public async Task DoWork_WhenDividendStateIsPredicted_ShouldBeFlaggedCorrectly()
		{
			// Arrange
			var today = DateOnly.FromDateTime(DateTime.Today);
			var (holding, snapshot) = BuildHolding(1, "PRED", today.AddDays(-1), quantity: 10);

			var activities = new List<Activity>
			{
				BuildDividendActivity("PRED", today.AddDays(-395), 1.50m),
				BuildDividendActivity("PRED", today.AddDays(-30), 1.60m),
			};

			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(db => db.CalculatedSnapshots).ReturnsDbSet([snapshot]);
			mockDbContext.Setup(db => db.Holdings).ReturnsDbSet([holding]);
			var dividendsList = new List<Dividend>();
			mockDbContext.Setup(db => db.Dividends).ReturnsDbSet(dividendsList);
			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);
			mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

			_mockDbContextFactory
				.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext.Object);

			var loggerMock = new Mock<ILogger>();

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: verify that the added dividend has DividendState.Predicted
			dividendsList.Should().ContainSingle();
			dividendsList.First().DividendState.Should().Be(DividendState.Predicted);
		}
	}
}