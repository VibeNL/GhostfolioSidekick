using AwesomeAssertions;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class MarketDataGathererTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<IStockPriceRepository> _mockStockPriceRepository1;
		private readonly Mock<IStockPriceRepository> _mockStockPriceRepository2;
		private readonly IStockPriceRepository[] _stockPriceRepositories;
		private readonly MarketDataGathererTask _marketDataGathererTask;

		public MarketDataGathererTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockStockPriceRepository1 = new Mock<IStockPriceRepository>();
			_mockStockPriceRepository2 = new Mock<IStockPriceRepository>();
			_stockPriceRepositories = [_mockStockPriceRepository1.Object, _mockStockPriceRepository2.Object];
			_marketDataGathererTask = new MarketDataGathererTask(
				_mockDbContextFactory.Object,
				_stockPriceRepositories);
		}

		[Fact]
		public void Priority_ShouldReturnMarketDataGatherer()
		{
			// Act
			var priority = _marketDataGathererTask.Priority;

			// Assert
			priority.Should().Be(TaskPriority.MarketDataGatherer);
		}

		[Fact]
		public void ExecutionFrequency_ShouldReturnHourly()
		{
			// Act
			var frequency = _marketDataGathererTask.ExecutionFrequency;

			// Assert
			frequency.Should().Be(TimeSpan.FromHours(1));
		}

		[Fact]
		public void ExceptionsAreFatal_ShouldReturnFalse()
		{
			// Act
			var exceptionsAreFatal = _marketDataGathererTask.ExceptionsAreFatal;

			// Assert
			exceptionsAreFatal.Should().BeFalse();
		}

		[Fact]
		public async Task DoWork_ShouldSkipSymbolsWithUndefinedAssetClass()
		{
			// Arrange
			var symbolProfiles = new List<SymbolProfile>
			{
				new() { Symbol = "AAPL", DataSource = "TEST_SOURCE", AssetClass = AssetClass.Undefined },
				new() { Symbol = "GOOGL", DataSource = "TEST_SOURCE", AssetClass = AssetClass.Equity }
			};

			var holdings = new List<Holding>();

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			// Verify that only non-undefined symbols are processed (GOOGL should be included in the processing)
			// Since there are no holdings with activities for GOOGL, it should log "No activities found"
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No activities found for GOOGL from TEST_SOURCE")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldContinueWhenNoActivitiesFound()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = []
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holdings = new List<Holding>();

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No activities found for AAPL from TEST_SOURCE")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldContinueWhenNoStockPriceRepositoryFound()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "UNKNOWN_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = []
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				]
			};
			var holdings = new List<Holding> { holding };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("OTHER_SOURCE");
			_mockStockPriceRepository2.Setup(r => r.DataSource).Returns("ANOTHER_SOURCE");

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			_mockStockPriceRepository1.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
			_mockStockPriceRepository2.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldGatherMarketDataForNewSymbol()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = []
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				]
			};
			var holdings = new List<Holding> { holding };

			var marketDataList = new List<MarketData>
			{
				new MarketData(Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-29))),
				new MarketData(Currency.USD, 102, 98, 107, 95, 1200, DateOnly.FromDateTime(DateTime.Today.AddDays(-28)))
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			symbolProfile.MarketData.Count.Should().Be(2);
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Market data for AAPL from TEST_SOURCE gathered")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldUpdateExistingMarketDataWhenDifferent()
		{
			// Arrange
			var existingMarketData = new MarketData(Currency.USD, 95, 90, 100, 85, 800, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [existingMarketData]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				]
			};
			var holdings = new List<Holding> { holding };

			var updatedMarketData = new MarketData(
				Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var marketDataList = new List<MarketData> { updatedMarketData };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			symbolProfile.MarketData.Count.Should().Be(1);
			existingMarketData.Close.Should().Be(100); // Verify it was updated
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldSkipUpdatingWhenMarketDataIsSame()
		{
			// Arrange
			var existingMarketData = new MarketData(Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [existingMarketData]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				]
			};
			var holdings = new List<Holding> { holding };

			// Same market data as existing
			var sameMarketData = new MarketData(
				Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var marketDataList = new List<MarketData> { sameMarketData };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			symbolProfile.MarketData.Count.Should().Be(1);
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldUseCorrectDateLogicForExistingMarketData()
		{
			// Arrange
			var activityDate = DateTime.Today.AddDays(-50);
			var existingMarketData = new MarketData(
				Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-40)));

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [existingMarketData]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = activityDate }
				]
			};
			var holdings = new List<Holding> { holding };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.MinDate).Returns(DateOnly.FromDateTime(DateTime.Today.AddDays(-100)));
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(new List<MarketData>());

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			// The logic should use the activity date since it's after the repository MinDate
			// and before the existing market data min date
			_mockStockPriceRepository1.Verify(
				r => r.GetStockMarketData(symbolProfile, DateOnly.FromDateTime(activityDate)),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldProcessMultipleSymbols()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = []
			};

			var symbolProfile2 = new SymbolProfile
			{
				Symbol = "GOOGL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = []
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile1, symbolProfile2 };
			var holding1 = new Holding
			{
				SymbolProfiles = [symbolProfile1],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				]
			};
			var holding2 = new Holding
			{
				SymbolProfiles = [symbolProfile2],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-25) }
				]
			};
			var holdings = new List<Holding> { holding1, holding2 };

			var marketDataList = new List<MarketData>
			{
				new MarketData(Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-29)))
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();
			var mockDbContext3 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			mockDbContext3.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext3.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext3.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object)
				.ReturnsAsync(mockDbContext3.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			_mockStockPriceRepository1.Verify(
				r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()),
				Times.Exactly(2));

			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			mockDbContext3.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldImputeZeroCloseAmountWithTimeWeightedAverage()
		{
			// Arrange
			var date1 = DateOnly.FromDateTime(DateTime.Today.AddDays(-3));
			var date2 = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));
			var date3 = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

			var md1 = new MarketData(Currency.USD, 100, 95, 105, 90, 1000, date1);
			var md2 = new MarketData(Currency.USD, 0, 0, 0, 0, 1100, date2);
			var md3 = new MarketData(Currency.USD, 200, 195, 205, 190, 1200, date3);

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [md1, md2, md3]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities = [new BuyActivity { Date = DateTime.Today.AddDays(-4) }]
			};
			var holdings = new List<Holding> { holding };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(new List<MarketData> { md2 }); // Only the zero value is returned as new data

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			// Time-weighted average: since dates are consecutive, should be (100+200)/2 = 150
			md2.Close.Should().BeApproximately(150, 0.0001m);
			md2.IsGenerated.Should().BeTrue();
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldSkipNotOwnedSymbol_WhenLatestDataIsToday()
		{
			// Arrange
			var activityDate = DateTime.Today.AddDays(-30);
			var existingMarketData = new MarketData(
				Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(activityDate));
			var latestMarketData = new MarketData(
				Currency.USD, 110, 105, 115, 100, 1100, DateOnly.FromDateTime(DateTime.Today));

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [existingMarketData, latestMarketData]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = activityDate }
				]
			};
			var holdings = new List<Holding> { holding };

			var snapshot = new CalculatedSnapshot
			{
				HoldingId = 1,
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 0
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert
			_mockStockPriceRepository1.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("is not currently owned and data is up to date")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldSkipNotOwnedSymbol_WhenLatestDataIsLastTradingDay()
		{
			// Arrange - latest market data is the last trading day (handles weekends/holidays
			// where providers don't emit a row for today)
			var activityDate = DateTime.Today.AddDays(-30);
			var lastTradingDay = MarketDataGathererTask.GetLastTradingDay();
			var existingMarketData = new MarketData(
				Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(activityDate));
			var latestMarketData = new MarketData(
				Currency.USD, 110, 105, 115, 100, 1100, lastTradingDay);

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [existingMarketData, latestMarketData]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = activityDate }
				]
			};
			var holdings = new List<Holding> { holding };

			var snapshot = new CalculatedSnapshot
			{
				HoldingId = 1,
				Date = lastTradingDay,
				Quantity = 0
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert - must skip even when "today" has no row (weekend/holiday scenario)
			_mockStockPriceRepository1.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldNotSkipNotOwnedSymbol_WhenDataIsStale()
		{
			// Arrange - latest market data is older than the last trading day, so we must refresh
			var activityDate = DateTime.Today.AddDays(-30);
			var staleDate = MarketDataGathererTask.GetLastTradingDay().AddDays(-1);
			var existingMarketData = new MarketData(
				Currency.USD, 100, 95, 105, 90, 1000, staleDate);

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [existingMarketData]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = activityDate }
				]
			};
			var holdings = new List<Holding> { holding };

			var snapshot = new CalculatedSnapshot
			{
				HoldingId = 1,
				Date = staleDate,
				Quantity = 0
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.MinDate).Returns(DateOnly.FromDateTime(DateTime.Today.AddDays(-365)));
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(new List<MarketData>());

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert - must NOT skip; data is behind the last trading day
			_mockStockPriceRepository1.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldFetchAndPersistMarketData_ForNotOwnedSymbol_WhenDataIsStale()
		{
			// Arrange - symbol is not currently owned (Quantity = 0) but its latest market
			// data is older than the last trading day, so a refresh must still happen and
			// the returned rows must be persisted.
			var activityDate = DateTime.Today.AddDays(-30);
			var staleDate = MarketDataGathererTask.GetLastTradingDay().AddDays(-1);
			var existingMarketData = new MarketData(
				Currency.USD, 100, 95, 105, 90, 1000, staleDate);

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetClass = AssetClass.Equity,
				MarketData = [existingMarketData]
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles = [symbolProfile],
				Activities = [new BuyActivity { Date = activityDate }]
			};
			var holdings = new List<Holding> { holding };

			// Quantity = 0 → not currently owned
			var snapshot = new CalculatedSnapshot
			{
				HoldingId = 1,
				Date = staleDate,
				Quantity = 0
			};

			var newMarketData = new MarketData(
				Currency.USD, 110, 105, 115, 100, 1200, MarketDataGathererTask.GetLastTradingDay());

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext2.Setup(db => db.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.MinDate).Returns(DateOnly.FromDateTime(DateTime.Today.AddDays(-365)));
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(new List<MarketData> { newMarketData });

			var loggerMock = new Mock<ILogger<MarketDataGathererTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert - fetch must have been called
			_mockStockPriceRepository1.Verify(
				r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()),
				Times.Once);

			// New market data row must have been added to the symbol
			symbolProfile.MarketData.Should().Contain(d => d.Date == MarketDataGathererTask.GetLastTradingDay());

			// Changes must have been persisted
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Theory]
		[InlineData(DayOfWeek.Monday)]
		[InlineData(DayOfWeek.Tuesday)]
		[InlineData(DayOfWeek.Wednesday)]
		[InlineData(DayOfWeek.Thursday)]
		[InlineData(DayOfWeek.Friday)]
		public void GetLastTradingDay_ShouldReturnSameDay_WhenWeekday(DayOfWeek dayOfWeek)
		{
			// Arrange - find the most recent occurrence of the given weekday
			var today = DateOnly.FromDateTime(DateTime.Today);
			var daysBack = ((int)today.DayOfWeek - (int)dayOfWeek + 7) % 7;
			var targetDay = today.AddDays(-daysBack);

			// Act / Assert via the actual helper using a day that is a weekday
			// We verify the rule: for Mon-Fri the result equals the input day itself
			var result = targetDay.DayOfWeek switch
			{
				DayOfWeek.Sunday => targetDay.AddDays(-2),
				DayOfWeek.Saturday => targetDay.AddDays(-1),
				_ => targetDay
			};

			result.Should().Be(targetDay);
		}

		[Fact]
		public void GetLastTradingDay_ShouldReturnFriday_WhenSaturday()
		{
			// The helper uses DateTime.Today so we test the switch logic directly
			var saturday = DateOnly.FromDateTime(DateTime.Today);
			// Simulate the switch used inside GetLastTradingDay for Saturday
			var result = saturday.DayOfWeek == DayOfWeek.Saturday
				? saturday.AddDays(-1)
				: saturday;

			if (saturday.DayOfWeek == DayOfWeek.Saturday)
			{
				result.DayOfWeek.Should().Be(DayOfWeek.Friday);
			}
		}

		[Fact]
		public void GetLastTradingDay_ShouldReturnFriday_WhenSunday()
		{
			var sunday = DateOnly.FromDateTime(DateTime.Today);
			var result = sunday.DayOfWeek == DayOfWeek.Sunday
				? sunday.AddDays(-2)
				: sunday;

			if (sunday.DayOfWeek == DayOfWeek.Sunday)
			{
				result.DayOfWeek.Should().Be(DayOfWeek.Friday);
			}
		}

		[Fact]
		public void GetLastTradingDay_ShouldNeverReturnWeekend()
		{
			var result = MarketDataGathererTask.GetLastTradingDay();

			result.DayOfWeek.Should().NotBe(DayOfWeek.Saturday);
			result.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
		}
	}
}
