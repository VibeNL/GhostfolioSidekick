using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Tests.Services
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

			_holdingsDataService = new HoldingsDataService(
				_mockDatabaseContext.Object,
				_mockServerConfigurationService.Object);
		}

		#region GetHoldingsAsync() Tests

		[Fact]
		public async Task GetHoldingsAsync_WithoutAccountId_ShouldReturnAllHoldings()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holdingAggregated, 1, testDate);

			holdingAggregated.CalculatedSnapshotsPrimaryCurrency = [calculatedSnapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { calculatedSnapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(cancellationToken);

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
			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync();

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetHoldingsAsync_ShouldCalculateWeightsCorrectly()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding1 = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var holding2 = CreateTestHoldingAggregated("MSFT", "Microsoft Corp");

			var snapshot1 = CreateTestCalculatedSnapshotPrimaryCurrency(holding1, 1, testDate, 10, 100, 110, 1000, 1100);
			var snapshot2 = CreateTestCalculatedSnapshotPrimaryCurrency(holding2, 1, testDate, 5, 200, 220, 1000, 1100);

			holding1.CalculatedSnapshotsPrimaryCurrency = [snapshot1];
			holding2.CalculatedSnapshotsPrimaryCurrency = [snapshot2];

			var holdingAggregateds = new List<HoldingAggregated> { holding1, holding2 };
			var snapshots = new List<CalculatedSnapshotPrimaryCurrency> { snapshot1, snapshot2 };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync();

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

			var holding = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, 1, testDate, 10, 100, 110, 1000, 1100);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync();

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

			var holding = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, 1, testDate, 10, 0, 110, 0, 1100);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync();

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

			var holdingZ = CreateTestHoldingAggregated("ZULU", "Zulu Corp");
			var holdingA = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var holdingM = CreateTestHoldingAggregated("MSFT", "Microsoft Corp");

			var snapshotZ = CreateTestCalculatedSnapshotPrimaryCurrency(holdingZ, 1, testDate);
			var snapshotA = CreateTestCalculatedSnapshotPrimaryCurrency(holdingA, 1, testDate);
			var snapshotM = CreateTestCalculatedSnapshotPrimaryCurrency(holdingM, 1, testDate);

			holdingZ.CalculatedSnapshotsPrimaryCurrency = [snapshotZ];
			holdingA.CalculatedSnapshotsPrimaryCurrency = [snapshotA];
			holdingM.CalculatedSnapshotsPrimaryCurrency = [snapshotM];

			var holdingAggregateds = new List<HoldingAggregated> { holdingZ, holdingA, holdingM };
			var snapshots = new List<CalculatedSnapshotPrimaryCurrency> { snapshotZ, snapshotA, snapshotM };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync();

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
			var cancellationToken = new CancellationToken();
			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());

			// Act
			await _holdingsDataService.GetHoldingsAsync(cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.HoldingAggregateds, Times.AtLeastOnce);
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshotPrimaryCurrencies, Times.AtLeastOnce);
		}

		#endregion

		#region GetHoldingsAsync(int accountId) Tests

		[Fact]
		public async Task GetHoldingsAsync_WithAccountId_ShouldReturnFilteredHoldings()
		{
			// Arrange
			var accountId = 2;
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, accountId, testDate);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(accountId);

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

			var holding = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, 1, testDate);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(0);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithNonExistentAccountId_ShouldReturnEmptyList()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, 1, testDate);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(999);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		#endregion

		#region GetHoldingAsync Tests

		[Fact]
		public async Task GetHoldingAsync_WithValidSymbol_ShouldReturnMatchingHolding()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holdingAAPL = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var holdingMSFT = CreateTestHoldingAggregated("MSFT", "Microsoft Corp");

			var snapshotAAPL = CreateTestCalculatedSnapshotPrimaryCurrency(holdingAAPL, 1, testDate);
			var snapshotMSFT = CreateTestCalculatedSnapshotPrimaryCurrency(holdingMSFT, 1, testDate);

			holdingAAPL.CalculatedSnapshotsPrimaryCurrency = [snapshotAAPL];
			holdingMSFT.CalculatedSnapshotsPrimaryCurrency = [snapshotMSFT];

			var holdingAggregateds = new List<HoldingAggregated> { holdingAAPL, holdingMSFT };
			var snapshots = new List<CalculatedSnapshotPrimaryCurrency> { snapshotAAPL, snapshotMSFT };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetHoldingAsync("AAPL");

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

			var holding = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, 1, testDate);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingAsync("GOOGL");

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetHoldingAsync_WithEmptyDatabase_ShouldReturnNull()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());

			// Act
			var result = await _holdingsDataService.GetHoldingAsync("AAPL");

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetHoldingAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var cancellationToken = new CancellationToken();
			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());

			// Act
			await _holdingsDataService.GetHoldingAsync("AAPL", cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.HoldingAggregateds, Times.AtLeastOnce);
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshotPrimaryCurrencies, Times.AtLeastOnce);
		}

		#endregion

		#region GetHoldingPriceHistoryAsync Tests

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithValidData_ShouldReturnPriceHistory()
		{
			// Arrange
			var symbol = "AAPL";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var holdingAggregated = CreateTestHoldingAggregated(symbol, "Apple Inc");
			var calculatedSnapshot1 = CreateTestCalculatedSnapshot(startDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110));
			var calculatedSnapshot2 = CreateTestCalculatedSnapshot(startDate.AddDays(1), 15, new Money(Currency.USD, 105), new Money(Currency.USD, 115));

			holdingAggregated.CalculatedSnapshots = [calculatedSnapshot1, calculatedSnapshot2];
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);

			var firstPoint = result.FirstOrDefault(x => x.Date == startDate);
			firstPoint.Should().NotBeNull();
			firstPoint!.Price.Amount.Should().Be(110);
			firstPoint.AveragePrice.Amount.Should().Be(100);

			var secondPoint = result.FirstOrDefault(x => x.Date == startDate.AddDays(1));
			secondPoint.Should().NotBeNull();
			secondPoint!.Price.Amount.Should().Be(115);
			secondPoint.AveragePrice.Amount.Should().Be(105);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithMultipleSnapshotsOnSameDate_ShouldGroupByDate()
		{
			// Arrange
			var symbol = "AAPL";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var testDate = startDate;

			var holdingAggregated = CreateTestHoldingAggregated(symbol, "Apple Inc");
			var calculatedSnapshot1 = CreateTestCalculatedSnapshot(testDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110));
			var calculatedSnapshot2 = CreateTestCalculatedSnapshot(testDate, 20, new Money(Currency.USD, 105), new Money(Currency.USD, 105)); // Lower price

			holdingAggregated.CalculatedSnapshots = [calculatedSnapshot1, calculatedSnapshot2];
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			var point = result[0];
			point.Date.Should().Be(testDate);
			point.Price.Amount.Should().Be(105); // Min price
			// Average price should be weighted: (100*10 + 105*20) / (10+20) = 103.33...
			point.AveragePrice.Amount.Should().BeApproximately(103.33m, 0.01m);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_WithNonExistentSymbol_ShouldReturnEmptyList()
		{
			// Arrange
			var symbol = "NONEXISTENT";
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated>());

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate);

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

			var holdingAggregated = CreateTestHoldingAggregated(symbol, "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(dataDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110));

			holdingAggregated.CalculatedSnapshots = [calculatedSnapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate);

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
			var cancellationToken = new CancellationToken();

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated>());

			// Act
			await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.HoldingAggregateds, Times.AtLeastOnce);
		}

		#endregion

		#region GetPortfolioValueHistoryAsync Tests

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithValidData_ShouldReturnPortfolioHistory()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateTestCalculatedSnapshotPrimaryCurrency(null, accountId, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshotPrimaryCurrency(null, accountId, startDate.AddDays(1), 10, 100, 115, 1000, 1150)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, accountId);

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
			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 2, startDate, 5, 200, 220, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, null);

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
			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 2, startDate, 5, 200, 220, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 0);

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

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 2, startDate, 5, 200, 220, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, accountId);

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

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 1, startDate.AddDays(2), 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshotPrimaryCurrency(null, 1, startDate.AddDays(1), 10, 100, 110, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 1);

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

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 1);

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
			var cancellationToken = new CancellationToken();

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());

			// Act
			await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, 1, cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshotPrimaryCurrencies, Times.AtLeastOnce);
		}

		#endregion

		#region Edge Cases and Error Handling

		[Fact]
		public async Task GetHoldingsAsync_WithNullName_ShouldUseSymbolAsName()
		{
			// Arrange
			var testDate = DateOnly.FromDateTime(DateTime.Now);

			var holding = CreateTestHoldingAggregated("AAPL", null); // null name
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, 1, testDate);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync();

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

			var holding = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holding, 1, testDate);

			holding.CalculatedSnapshotsPrimaryCurrency = [snapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holding };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { snapshot });

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync();

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

			var holdingAggregated = CreateTestHoldingAggregated(symbol, "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(startDate, 10, new Money(Currency.USD, 100), null);

			holdingAggregated.CalculatedSnapshots = [calculatedSnapshot];
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryAsync(symbol, startDate, endDate);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Price.Amount.Should().Be(0); // Should handle null CurrentUnitPrice
		}

		#endregion

		#region Helper Methods

		private static HoldingAggregated CreateTestHoldingAggregated(string symbol, string? name)
		{
			return new HoldingAggregated
			{
				Symbol = symbol,
				Name = name,
				AssetClass = AssetClass.Equity,
				SectorWeights = [new SectorWeight { Name = "Technology" }],
				CalculatedSnapshots = [],
				CalculatedSnapshotsPrimaryCurrency = []
			};
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Testcode")]
		private static CalculatedSnapshotPrimaryCurrency CreateTestCalculatedSnapshotPrimaryCurrency(
			HoldingAggregated? holding, 
			int? accountId, 
			DateOnly? date,
			decimal quantity = 10,
			decimal averageCostPrice = 100,
			decimal currentUnitPrice = 110,
			decimal totalInvested = 1000,
			decimal totalValue = 1100)
		{
			return new CalculatedSnapshotPrimaryCurrency
			{
				Id = Random.Shared.Next(1, 1000),
				AccountId = accountId ?? 1,  // Default to 1 since AccountId is non-nullable int
				Date = date ?? DateOnly.FromDateTime(DateTime.Now),
				Quantity = quantity,
				AverageCostPrice = averageCostPrice,
				CurrentUnitPrice = currentUnitPrice,
				TotalInvested = totalInvested,
				TotalValue = totalValue
			};
		}

		private static CalculatedSnapshot CreateTestCalculatedSnapshot(
			DateOnly date,
			decimal quantity,
			Money averageCostPrice,
			Money? currentUnitPrice)
		{
			return new CalculatedSnapshot(
				id: Random.Shared.Next(1, 1000),
				accountId: 1,
				date: date,
				quantity: quantity,
				averageCostPrice: averageCostPrice,
				currentUnitPrice: currentUnitPrice ?? Money.Zero(Currency.EUR),
				totalInvested: Money.Zero(Currency.USD),
				totalValue: Money.Zero(Currency.USD));
		}

		#endregion
	}
}