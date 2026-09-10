using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
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
			_mockServerConfigurationService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

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
           result[0].Symbols.FirstOrDefault().Should().Be("AAPL");
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
           result[0].Symbols.FirstOrDefault().Should().Be("AAPL");
           result[1].Symbols.FirstOrDefault().Should().Be("MSFT");
           result[2].Symbols.FirstOrDefault().Should().Be("ZULU");
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
           result[0].Symbols.FirstOrDefault().Should().Be("AAPL");
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
           result!.Symbols.FirstOrDefault().Should().Be("AAPL");
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
		public async Task GetHoldingPriceHistoryBulkAsync_WithMultipleSymbols_ShouldReturnHistoryPerSymbol()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var holdingAapl = CreateTestHolding("AAPL", "Apple Inc");
			holdingAapl.CalculatedSnapshots = [CreateTestCalculatedSnapshot(startDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110))];

			var holdingMsft = CreateTestHolding("MSFT", "Microsoft Corp");
			holdingMsft.CalculatedSnapshots = [CreateTestCalculatedSnapshot(startDate.AddDays(1), 5, new Money(Currency.USD, 200), new Money(Currency.USD, 220))];

			var holdingGoo = CreateTestHolding("GOOG", "Alphabet Inc");
			holdingGoo.CalculatedSnapshots = [CreateTestCalculatedSnapshot(startDate.AddDays(2), 1, new Money(Currency.USD, 300), new Money(Currency.USD, 310))];

			var holdings = new List<Holding> { holdingAapl, holdingMsft, holdingGoo };
			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryBulkAsync(
				new[] { "AAPL", "MSFT" }, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().HaveCount(2);
			result["AAPL"].Should().HaveCount(1);
			result["AAPL"][0].Date.Should().Be(startDate);
			result["AAPL"][0].Price.Should().Be(110);
			result["MSFT"].Should().HaveCount(1);
			result["MSFT"][0].Date.Should().Be(startDate.AddDays(1));
			result["MSFT"][0].Price.Should().Be(220);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryBulkAsync_WithEmptySymbols_ShouldReturnEmptyDictionary()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryBulkAsync(
				Array.Empty<string>(), startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetHoldingPriceHistoryBulkAsync_WithDateRangeFilter_ShouldExcludeSnapshotsOutsideRange()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var holding = CreateTestHolding("AAPL", "Apple Inc");
			holding.CalculatedSnapshots = [
				CreateTestCalculatedSnapshot(new DateOnly(2022, 12, 15), 10, new Money(Currency.USD, 90), new Money(Currency.USD, 95)),
				CreateTestCalculatedSnapshot(startDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110)),
				CreateTestCalculatedSnapshot(new DateOnly(2023, 2, 15), 10, new Money(Currency.USD, 120), new Money(Currency.USD, 130))
			];

			var holdings = new List<Holding> { holding };
			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _holdingsDataService.GetHoldingPriceHistoryBulkAsync(
				new[] { "AAPL" }, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().HaveCount(1);
			result["AAPL"].Should().HaveCount(1);
			result["AAPL"][0].Date.Should().Be(startDate);
			result["AAPL"][0].Price.Should().Be(110);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryBulkAsync_OnRealSqlite_ShouldTranslateAndReturnPerSymbolHistory()
		{
			// Arrange: real SQLite context so EF query translation is exercised (WASM runtime uses the same provider)
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			using var testDatabase = new SqliteTestDatabase();
			var holdingAapl = CreateTestHolding("AAPL", "Apple Inc");
			holdingAapl.CalculatedSnapshots = [CreateTestCalculatedSnapshot(holdingAapl, startDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110))];

			var holdingMsft = CreateTestHolding("MSFT", "Microsoft Corp");
			holdingMsft.CalculatedSnapshots = [CreateTestCalculatedSnapshot(holdingMsft, startDate.AddDays(1), 5, new Money(Currency.USD, 200), new Money(Currency.USD, 220))];

			using (var seedContext = testDatabase.CreateContext())
			{
				seedContext.Holdings.AddRange(holdingAapl, holdingMsft);
				await seedContext.SaveChangesAsync(CancellationToken.None);
			}

			var dbFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(() => testDatabase.CreateContext());
			var service = new HoldingsDataService(dbFactory.Object, _mockServerConfigurationService.Object);

			// Act
			var result = await service.GetHoldingPriceHistoryBulkAsync(
				new[] { "AAPL", "MSFT" }, startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().HaveCount(2);
			result["AAPL"].Should().HaveCount(1);
			result["AAPL"][0].Date.Should().Be(startDate);
			result["AAPL"][0].Price.Should().Be(110);
			result["MSFT"].Should().HaveCount(1);
			result["MSFT"][0].Date.Should().Be(startDate.AddDays(1));
			result["MSFT"][0].Price.Should().Be(220);
		}

		[Fact]
		public async Task GetHoldingPriceHistoryBulkAsync_OnRealSqlite_ShouldMatchPerSymbolResults()
		{
			// Arrange: parity with GetHoldingPriceHistoryAsync — a holding with multiple profiles contributes to each symbol
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			using var testDatabase = new SqliteTestDatabase();
			var holdingAaplMsft = CreateTestHolding("AAPL", "Apple Inc");
			holdingAaplMsft.SymbolProfiles.Add(CreateSymbolProfile("MSFT"));
			holdingAaplMsft.CalculatedSnapshots = [CreateTestCalculatedSnapshot(holdingAaplMsft, startDate, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 110))];

			// Second holding also matches AAPL (different datasource keeps the SymbolProfile key unique);
			// it has a snapshot on the same date as holdingAaplMsft to cover cross-holding merging per date
			var holdingAapl2 = CreateTestHolding("AAPL", "Apple Inc");
			holdingAapl2.SymbolProfiles[0].DataSource = Datasource.COINGECKO;
			holdingAapl2.CalculatedSnapshots = [
				CreateTestCalculatedSnapshot(holdingAapl2, startDate, 5, new Money(Currency.USD, 90), new Money(Currency.USD, 95)),
				CreateTestCalculatedSnapshot(holdingAapl2, startDate.AddDays(1), 5, new Money(Currency.USD, 85), new Money(Currency.USD, 90))];

			using (var seedContext = testDatabase.CreateContext())
			{
				seedContext.Holdings.AddRange(holdingAaplMsft, holdingAapl2);
				await seedContext.SaveChangesAsync(CancellationToken.None);
			}

			var dbFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(() => testDatabase.CreateContext());
			var service = new HoldingsDataService(dbFactory.Object, _mockServerConfigurationService.Object);

			// Act
			var symbols = new[] { "AAPL", "MSFT", "GOOG" };
			var bulkResult = await service.GetHoldingPriceHistoryBulkAsync(symbols, startDate, endDate, CancellationToken.None);

			// Assert: identical to calling the per-symbol method for each symbol
			foreach (var symbol in symbols)
			{
				var expected = await service.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, CancellationToken.None);
				bulkResult.Should().ContainKey(symbol);

				var actualPoints = bulkResult[symbol].Select(p => new { p.Date, p.Price, p.AveragePrice }).ToList();
				var expectedPoints = expected.Select(p => new { p.Date, p.Price, p.AveragePrice }).ToList();
				actualPoints.Should().Equal(expectedPoints);
			}

			bulkResult["GOOG"].Should().BeEmpty(); // requested but no holding has this profile

			// Same-date snapshots from both AAPL holdings merge into one point with Price = Min across holdings
			bulkResult["AAPL"].Should().HaveCount(2);
			bulkResult["AAPL"][0].Price.Should().Be(95); // min of 110 (holdingAaplMsft) and 95 (holdingAapl2) on startDate
		}

		private sealed class SqliteTestDatabase : IDisposable
		{
			public DbContextOptions<DatabaseContext> Options { get; }

			private readonly string _filePath;

			public SqliteTestDatabase()
			{
				_filePath = $"test_holdings_bulk_{Guid.NewGuid():N}.db";
				Options = new DbContextOptionsBuilder<DatabaseContext>()
					.UseSqlite($"Data Source={_filePath}")
					.Options;

				using var context = CreateContext();
				context.Database.EnsureCreated();
			}

			public DatabaseContext CreateContext() => new(Options);

			public void Dispose()
			{
				try { File.Delete(_filePath); } catch (IOException) { /* best effort */ }
			}
		}

		private static SymbolProfile CreateSymbolProfile(string symbol)
		{
			return new SymbolProfile(
				symbol: symbol,
				name: null,
				identifiers: [],
				currency: Currency.USD,
				dataSource: "YAHOO",
				assetClass: AssetClass.Equity,
				assetSubClass: null,
				countries: [],
				sectors: []);
		}

		private static CalculatedSnapshot CreateTestCalculatedSnapshot(
			Holding holding,
			DateOnly date,
			decimal quantity,
			Money averageCostPrice,
			Money currentUnitPrice)
		{
			return new CalculatedSnapshot(
				id: Guid.NewGuid(),
				accountId: 1,
				date: date,
				quantity: quantity,
				currency: Currency.USD,
				averageCostPrice: averageCostPrice.Amount,
				currentUnitPrice: currentUnitPrice.Amount,
				totalInvested: 0,
				totalValue: 0)
			{
				Holding = holding,
			};
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
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

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
		public async Task GetPortfolioValueHistoryAsync_WithNullAccountId_ShouldReturnAllAccounts()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(1, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(2, startDate, 5, 200, 220, 1000, 1100)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, null, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1); // Grouped by date
			result[0].Value.Should().Be(2200); // null means all accounts
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
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

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
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

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
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

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
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

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
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

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
			_mockServerConfigurationService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.EUR);

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

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithBalanceOnlyDate_ShouldAppearInOutput()
		{
			// Arrange: snapshot on Jan 1, balance change on Jan 3 (no snapshot that day), snapshot again on Jan 5
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(accountId, startDate, 10, 100, 110, 1000, 1100),
				CreateTestCalculatedSnapshot(accountId, startDate.AddDays(4), 10, 100, 120, 1000, 1200)
			};

			var balances = new List<Balance>
			{
				new Balance(startDate, new Money(Currency.USD, 500)) { AccountId = accountId },
				new Balance(startDate.AddDays(2), new Money(Currency.USD, 750)) { AccountId = accountId }
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, accountId, CancellationToken.None);

			// Assert: three distinct dates — Jan 1 (snapshot), Jan 3 (balance-only), Jan 5 (snapshot)
			result.Should().NotBeNull();
			result.Should().HaveCount(3);

			var jan1 = result[0];
			jan1.Date.Should().Be(startDate);
			jan1.Value.Should().Be(1100);
			jan1.Balance.Should().Be(500);

			var jan3 = result[1];
			jan3.Date.Should().Be(startDate.AddDays(2));
			jan3.Value.Should().Be(1100); // forward-filled from Jan 1 snapshot
			jan3.Balance.Should().Be(750);

			var jan5 = result[2];
			jan5.Date.Should().Be(startDate.AddDays(4));
			jan5.Value.Should().Be(1200);
			jan5.Balance.Should().Be(750); // forward-filled from Jan 3 balance
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithBalanceBeforeStartDate_ShouldForwardFillIntoRange()
		{
			// Arrange: balance record before startDate should still be forward-filled into the chart range
			var startDate = new DateOnly(2023, 1, 5);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateTestCalculatedSnapshot(accountId, startDate, 10, 100, 110, 1000, 1100)
			};

			var balances = new List<Balance>
			{
				new Balance(new DateOnly(2023, 1, 1), new Money(Currency.USD, 300)) { AccountId = accountId }
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _holdingsDataService.GetPortfolioValueHistoryAsync(startDate, endDate, accountId, CancellationToken.None);

			// Assert: the pre-range balance is forward-filled onto Jan 5
			result.Should().HaveCount(1);
			result[0].Date.Should().Be(startDate);
			result[0].Balance.Should().Be(300);
		}

		private static int _holdingIdCounter = 0;

		private static Holding CreateTestHolding(string symbol, string? name)
		{
			var holding = new Holding { 
				Id = ++_holdingIdCounter, 
				SymbolProfiles = [
					new SymbolProfile(
						symbol: symbol,
						name: name, 
						identifiers: [], 
						currency: Currency.USD,
						dataSource: "YAHOO",
						assetClass: AssetClass.Equity, 
						assetSubClass: null, 
						countries: [], 
						sectors: [new SectorWeight { Name = "Technology" }])], CalculatedSnapshots = [] }; 
			return holding;
		}

		private static CalculatedSnapshot CreateTestCalculatedSnapshot(
			int? accountId,
			DateOnly? date,
			decimal quantity = 10,
			decimal averageCostPrice = 100,
			decimal currentUnitPrice = 110,
			decimal totalInvested = 1000,
			decimal totalValue = 1100)
		{
			return new CalculatedSnapshot(id: Guid.NewGuid(), accountId: accountId ?? 1, date: date ?? DateOnly.FromDateTime(DateTime.Now), quantity: quantity, currency: Currency.USD, averageCostPrice: averageCostPrice, currentUnitPrice: currentUnitPrice, totalInvested: totalInvested, totalValue: totalValue)
			{
				Holding = new Holding { SymbolProfiles = [new SymbolProfile { Symbol = "TEST" }] },
			};
		}

		private static CalculatedSnapshot CreateTestCalculatedSnapshot(
			DateOnly date,
			decimal quantity,
			Money averageCostPrice,
			Money? currentUnitPrice)
		{
			return new CalculatedSnapshot(
				id: Guid.NewGuid(),
				accountId: 1,
				date: date,
				quantity: quantity,
				currency: Currency.USD, averageCostPrice: averageCostPrice.Amount, currentUnitPrice: currentUnitPrice?.Amount ?? 0, totalInvested: 0, totalValue: 0)
			{
				Holding = new Holding { SymbolProfiles = [new SymbolProfile { Symbol = "TEST" }] },
			};
		}
	}
}





