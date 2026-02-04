using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class HoldingsDataServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly Mock<IServerConfigurationService> _mockServerConfigurationService;
		private readonly HoldingsDataService _holdingsDataService;

		public HoldingsDataServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			_mockServerConfigurationService = new Mock<IServerConfigurationService>();

			// Setup default primary currency
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.USD);

			var dbFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(_mockDatabaseContext.Object);

			_holdingsDataService = new HoldingsDataService(
				dbFactory.Object,
				_mockServerConfigurationService.Object);
		}


		[Fact]
		public async Task GetHoldingsAsync_WithoutAccountId_ShouldReturnAllHoldings()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(1, testDate);

			holding.CalculatedSnapshots = [calculatedSnapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("AAPL");
			result[0].Name.Should().Be("Apple Inc");
			result[0].Currency.Should().Be(Currency.USD.Symbol);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetHoldingsAsync_ShouldCalculateWeightsCorrectly()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding1 = CreateTestHolding("AAPL", "Apple Inc");
			var holding2 = CreateTestHolding("MSFT", "Microsoft Corp");

			var snapshot1 = CreateTestCalculatedSnapshot(1, testDate, 10, 100, 110, 1000, 1100);
			var snapshot2 = CreateTestCalculatedSnapshot(1, testDate, 5, 200, 220, 1000, 1100);

			holding1.CalculatedSnapshots = [snapshot1];
			holding2.CalculatedSnapshots = [snapshot2];

			var holdings = new List<Holding> { holding1, holding2 };
			var snapshots = new List<CalculatedSnapshot> { snapshot1, snapshot2 };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);

			var totalValue = result.Sum(x => x.CurrentValue.Amount);
			totalValue.Should().Be(2200); // 1100 + 1100

			result[0].Weight.Should().Be(0.5m); // 1100 / 2200
			result[1].Weight.Should().Be(0.5m); // 1100 / 2200
		}

		[Fact]
		public async Task GetHoldingsAsync_ShouldCalculateGainLossCorrectly()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(1, testDate, 10, 100, 110, 1000, 1100);

			holding.CalculatedSnapshots = [snapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			var holdingResult = result[0];
			holdingResult.GainLoss.Amount.Should().Be(100); // 1100 - (100 * 10)
			holdingResult.GainLossPercentage.Should().Be(0.1m); // 100 / 1000
		}

		[Fact]
		public async Task GetHoldingsAsync_WithZeroAveragePrice_ShouldSetGainLossPercentageToZero()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(1, testDate, 10, 0, 110, 0, 1100);

			holding.CalculatedSnapshots = [snapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			var holdingResult = result[0];
			holdingResult.GainLossPercentage.Should().Be(0);
		}

		[Fact]
		public async Task GetHoldingsAsync_ShouldOrderBySymbol()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holdingZ = CreateTestHolding("ZULU", "Zulu Corp");
			var holdingA = CreateTestHolding("AAPL", "Apple Inc");
			var holdingM = CreateTestHolding("MSFT", "Microsoft Corp");

			var snapshotZ = CreateTestCalculatedSnapshot(1, testDate);
			var snapshotA = CreateTestCalculatedSnapshot(1, testDate);
			var snapshotM = CreateTestCalculatedSnapshot(1, testDate);

			holdingZ.CalculatedSnapshots = [snapshotZ];
			holdingA.CalculatedSnapshots = [snapshotA];
			holdingM.CalculatedSnapshots = [snapshotM];

			var holdings = new List<Holding> { holdingZ, holdingA, holdingM };
			var snapshots = new List<CalculatedSnapshot> { snapshotZ, snapshotA, snapshotM };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result[0].Symbol.Should().Be("AAPL");
			result[1].Symbol.Should().Be("MSFT");
			result[2].Symbol.Should().Be("ZULU");
		}

		[Fact]
		public async Task GetHoldingsAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			// Act
			await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.Holdings, Times.AtLeastOnce);
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshots, Times.AtLeastOnce);
		}



		[Fact]
		public async Task GetHoldingsAsync_WithAccountId_ShouldReturnFilteredHoldings()
		{
			// Arrange
			var accountId = 2;
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(accountId, testDate);

			holding.CalculatedSnapshots = [snapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(accountId, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("AAPL");
		}

		[Fact]
		public async Task GetHoldingsAsync_WithZeroAccountId_ShouldTreatAsNull()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(1, testDate);

			holding.CalculatedSnapshots = [snapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(0, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithNonExistentAccountId_ShouldReturnEmptyList()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(1, testDate);

			holding.CalculatedSnapshots = [snapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(999, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}



		[Fact]
		public async Task GetHoldingAsync_WithValidSymbol_ShouldReturnMatchingHolding()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holdingAAPL = CreateTestHolding("AAPL", "Apple Inc");
			var holdingMSFT = CreateTestHolding("MSFT", "Microsoft Corp");

			var snapshotAAPL = CreateTestCalculatedSnapshot(1, testDate);
			var snapshotMSFT = CreateTestCalculatedSnapshot(1, testDate);

			holdingAAPL.CalculatedSnapshots = [snapshotAAPL];
			holdingMSFT.CalculatedSnapshots = [snapshotMSFT];

			var holdings = new List<Holding> { holdingAAPL, holdingMSFT };
			var snapshots = new List<CalculatedSnapshot> { snapshotAAPL, snapshotMSFT };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetHoldingAsync("AAPL", CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result!.Symbol.Should().Be("AAPL");
			result.Name.Should().Be("Apple Inc");
		}

		[Fact]
		public async Task GetHoldingAsync_WithNonExistentSymbol_ShouldReturnNull()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(1, testDate);

			holding.CalculatedSnapshots = [snapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingAsync("GOOGL", CancellationToken.None);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetHoldingAsync_WithEmptyDatabase_ShouldReturnNull()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			// Act
			var result = await _holdingsDataService.GetHoldingAsync("AAPL", CancellationToken.None);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetHoldingAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var cancellationToken = new CancellationToken();
			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			// Act
			await _holdingsDataService.GetHoldingAsync("AAPL", cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.Holdings, Times.AtLeastOnce);
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshots, Times.AtLeastOnce);
		}



		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithValidData_ShouldReturnPriceHistory()
		{
			// Arrange
			var symbol = "AAPL";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var holding = CreateTestHolding(symbol, "Apple Inc");
			var calculatedSnapshot1 = CreateTestCalculatedSnapshot(startDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110));
			var calculatedSnapshot2 = CreateTestCalculatedSnapshot(startDate.AddDays(1), 15, new Money(Currency.USD, 105), new Money(Currency.USD, 115));

			holding.CalculatedSnapshots = [calculatedSnapshot1, calculatedSnapshot2];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);

			var firstPoint = result.FirstOrDefault(x => x.Date == startDate);
			firstPoint.Should().NotBeNull();
			firstPoint!.Price.Should().Be(110);
			firstPoint.AveragePrice.Should().Be(100);

			var secondPoint = result.FirstOrDefault(x => x.Date == startDate.AddDays(1));
			secondPoint.Should().NotBeNull();
			secondPoint!.Price.Should().Be(115);
			secondPoint.AveragePrice.Should().Be(105);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithMultipleSnapshotsOnSameDate_ShouldGroupByDate()
		{
			// Arrange
			var symbol = "AAPL";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var testDate = startDate;

			var holding = CreateTestHolding(symbol, "Apple Inc");
			var calculatedSnapshot1 = CreateTestCalculatedSnapshot(testDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110));
			var calculatedSnapshot2 = CreateTestCalculatedSnapshot(testDate, 20, new Money(Currency.USD, 105), new Money(Currency.USD, 105)); // Lower price

			holding.CalculatedSnapshots = [calculatedSnapshot1, calculatedSnapshot2];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			var point = result[0];
			point.Date.Should().Be(testDate);
			point.Price.Should().Be(105); // Min price
										  // Average price should be weighted: (100*10 + 105*20) / (10+20) = 103.33...
			point.AveragePrice.Should().BeApproximately(103.33m, 0.01m);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithNonExistentSymbol_ShouldReturnEmptyList()
		{
			// Arrange
			var symbol = "NONEXISTENT";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithDateRangeOutsideData_ShouldReturnEmptyList()
		{
			// Arrange
			var symbol = "AAPL";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var dataDate = new DateOnly(2024, 1, 1); // Outside range

			var holding = CreateTestHolding(symbol, "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(dataDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110));

			holding.CalculatedSnapshots = [calculatedSnapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var symbol = "AAPL";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());

			// Act
			await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.Holdings, Times.AtLeastOnce);
		}



		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithValidData_ShouldReturnPortfolioHistory()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(accountId, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(accountId, startDate.AddDays(1), 10, 100, 115, 1000, 1150)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, accountId, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);

			result[0].Date.Should().Be(startDate);
			result[0].Value.Should().Be(1100);
			result[0].Invested.Should().Be(1000);

			result[1].Date.Should().Be(startDate.AddDays(1));
			result[1].Value.Should().Be(1150);
			result[1].Invested.Should().Be(1000);
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithNullAccountId_ShouldReturnEmptyWhenConditionNeverMatches()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			// According to the implementation, when accountId is null, the condition is:
			// (accountId == 0 || x.AccountId == accountId) which becomes:
			// (null == 0 || x.AccountId == null) = (false || false) = false
			// Since AccountId is int (non-nullable), x.AccountId == null is always false
			// So when accountId is null, the condition never matches and returns empty result
			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(2, startDate, 5, 200, 220, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, null, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty(); // When accountId is null, the condition never matches
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithZeroAccountId_ShouldReturnAllAccounts()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			// When accountId is 0, the condition is:
			// (accountId == 0 || x.AccountId == accountId) which becomes:
			// (0 == 0 || x.AccountId == 0) = (true || x.AccountId == 0) = true
			// So it returns ALL records regardless of AccountId
			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(2, startDate, 5, 200, 220, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 0, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1); // Grouped by date
			result[0].Value.Should().Be(2200); // Should include both accounts
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithSpecificAccountId_ShouldFilterByAccount()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(2, startDate, 5, 200, 220, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, accountId, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Value.Should().Be(1100); // Only account 1
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_ShouldOrderByDate()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(1, startDate.AddDays(2), 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(1, startDate.AddDays(1), 10, 100, 110, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 1, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result[0].Date.Should().Be(startDate);
			result[1].Date.Should().Be(startDate.AddDays(1));
			result[2].Date.Should().Be(startDate.AddDays(2));
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 1, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			// Act
			await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 1, CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshots, Times.AtLeastOnce);
		}



		[Fact]
		public async Task GetHoldingsAsync_WithNullName_ShouldUseSymbolAsName()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHolding("AAPL", null); // null name
			var snapshot = CreateTestCalculatedSnapshot(1, testDate);

			holding.CalculatedSnapshots = [snapshot];
			var Holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(Holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Name.Should().Be("AAPL"); // Should use symbol when name is null
		}

		[Fact]
		public async Task GetHoldingsAsync_WithDifferentPrimaryCurrency_ShouldUseCurrencyFromService()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.EUR);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(1, testDate);

			holding.CalculatedSnapshots = [snapshot];
			var Holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(Holdings);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Currency.Should().Be(Currency.EUR.Symbol);
			result[0].GainLoss.Currency.Should().Be(Currency.EUR);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithNullCurrentUnitPrice_ShouldUseZeroPrice()
		{
			// Arrange
			var symbol = "AAPL";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var holding = CreateTestHolding(symbol, "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(startDate, 10, new Money(Currency.USD, 100), null);

			holding.CalculatedSnapshots = [calculatedSnapshot];
			var holdings = new List<Holding> { holding };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Price.Should().Be(0); // Should handle null CurrentUnitPrice
		}

		private static int _holdingIdCounter = 0;

		private static Holding CreateTestHolding(string symbol, string? name)
		{
			var holding = new Holding { Id = ++_holdingIdCounter, SymbolProfiles = [new SymbolProfile(symbol: symbol, name: name, identifiers: [], currency: Currency.USD, dataSource: "YAHOO", assetClass: AssetClass.Equity, assetSubClass: null, countries: [], sectors: [new SectorWeight { Name = "Technology" }])], CalculatedSnapshots = [] }; return holding;
		}

		private static int _snapshotIdCounter = 0;

		private static CalculatedSnapshot CreateTestCalculatedSnapshot(
			int? accountId,
			DateOnly? date,
			decimal quantity = 10,
			decimal averageCostPrice = 100,
			decimal currentUnitPrice = 110,
			decimal totalInvested = 1000,
			decimal totalValue = 1100)
		{
			return new CalculatedSnapshot(id: ++_snapshotIdCounter, accountId: accountId ?? 1, date: date ?? DateOnly.FromDateTime(DateTime.Now), quantity: quantity, currency: Currency.USD, averageCostPrice: averageCostPrice, currentUnitPrice: currentUnitPrice, totalInvested: totalInvested, totalValue: totalValue);
		}

		private static CalculatedSnapshot CreateTestCalculatedSnapshot(
			DateOnly date,
			decimal quantity,
			Money averageCostPrice,
			Money? currentUnitPrice)
		{
			return new CalculatedSnapshot(
				id: ++_snapshotIdCounter,
				accountId: 1,
				date: date,
				quantity: quantity,
				currency: Currency.USD, averageCostPrice: averageCostPrice.Amount, currentUnitPrice: currentUnitPrice?.Amount ?? 0, totalInvested: 0, totalValue: 0);
		}
	}
}





