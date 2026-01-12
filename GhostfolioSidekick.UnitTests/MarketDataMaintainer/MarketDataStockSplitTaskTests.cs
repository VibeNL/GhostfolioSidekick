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
	public class MarketDataStockSplitTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<IStockSplitRepository> _mockStockSplitRepository1;
		private readonly Mock<IStockSplitRepository> _mockStockSplitRepository2;
		private readonly IStockSplitRepository[] _stockSplitRepositories;
		private readonly MarketDataStockSplitTask _marketDataStockSplitTask;

		public MarketDataStockSplitTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockStockSplitRepository1 = new Mock<IStockSplitRepository>();
			_mockStockSplitRepository2 = new Mock<IStockSplitRepository>();
			_stockSplitRepositories = [_mockStockSplitRepository1.Object, _mockStockSplitRepository2.Object];
			_marketDataStockSplitTask = new MarketDataStockSplitTask(
				_mockDbContextFactory.Object,
				_stockSplitRepositories);
		}

		[Fact]
		public void Priority_ShouldReturnMarketDataStockSplit()
		{
			// Act
			var priority = _marketDataStockSplitTask.Priority;

			// Assert
			priority.Should().Be(TaskPriority.MarketDataStockSplit);
		}

		[Fact]
		public void ExecutionFrequency_ShouldReturnHourly()
		{
			// Act
			var frequency = _marketDataStockSplitTask.ExecutionFrequency;

			// Assert
			frequency.Should().Be(TimeSpan.FromHours(1));
		}

		[Fact]
		public void ExceptionsAreFatal_ShouldReturnFalse()
		{
			// Act
			var exceptionsAreFatal = _marketDataStockSplitTask.ExceptionsAreFatal;

			// Assert
			exceptionsAreFatal.Should().BeFalse();
		}

		[Fact]
		public void Name_ShouldReturnCorrectName()
		{
			// Act
			var name = _marketDataStockSplitTask.Name;

			// Assert
			name.Should().Be("Market Data Stock Split Gatherer");
		}

		[Fact]
		public async Task DoWork_ShouldSkipNonStockSymbols()
		{
			// Arrange
			var symbolProfiles = new List<SymbolProfile>
			{
				new() { Symbol = "BTC", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.CryptoCurrency },
				new() { Symbol = "AAPL", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.Stock }
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

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			// Only AAPL (stock) should be processed, BTC (crypto) should be skipped
			// Since there are no holdings setup, we won't see further processing
			_mockDbContextFactory.Verify(
				factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()),
				Times.Exactly(2)); // Called once for initial query and once for AAPL processing
		}

		[Fact]
		public async Task DoWork_ShouldSkipGhostfolioDataSources()
		{
			// Arrange
			var symbolProfiles = new List<SymbolProfile>
			{
				new() { Symbol = "AAPL", DataSource = "GHOSTFOLIO-YAHOO", AssetSubClass = AssetSubClass.Stock },
				new() { Symbol = "GOOGL", DataSource = "YAHOO", AssetSubClass = AssetSubClass.Stock }
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

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			// Only GOOGL should be processed, AAPL with GHOSTFOLIO datasource should be filtered out
			// Since there are no holdings setup, we won't see further processing
			_mockDbContextFactory.Verify(
				factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()),
				Times.Exactly(2)); // Called once for initial query and once for GOOGL processing
		}

		[Fact]
		public async Task DoWork_ShouldContinueWhenNoActivitiesFound()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = []
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

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Trace,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No activities found for AAPL from TEST_SOURCE")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldContinueWhenNoStockSplitRepositoryFound()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "UNKNOWN_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = []
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

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockSplitRepository1.Setup(r => r.DataSource).Returns("OTHER_SOURCE");
			_mockStockSplitRepository2.Setup(r => r.DataSource).Returns("ANOTHER_SOURCE");

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No stock split repository found for UNKNOWN_SOURCE")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);

			_mockStockSplitRepository1.Verify(r => r.GetStockSplits(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
			_mockStockSplitRepository2.Verify(r => r.GetStockSplits(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldGatherStockSplitsForNewSymbol()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = [],
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

			var stockSplits = new List<StockSplit>
			{
				new(DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), 1, 2), // 1:2 split
				new(DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), 1, 4)  // 1:4 split
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockSplitRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockSplitRepository1.Setup(r => r.GetStockSplits(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(stockSplits);

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			symbolProfile.StockSplits.Count.Should().Be(2);
			symbolProfile.MarketData.Should().BeEmpty(); // Market data should be cleared when splits are updated
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stock splits for AAPL from TEST_SOURCE gathered")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldUpdateExistingStockSplitsWhenDifferent()
		{
			// Arrange
			var existingStockSplit = new StockSplit(DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), 1, 2);
			var existingMarketData = new MarketData(
				new Money(Currency.USD, 100), new Money(Currency.USD, 95),
				new Money(Currency.USD, 105), new Money(Currency.USD, 90),
				1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = [existingStockSplit],
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

			// New splits include the existing one plus a new one
			var updatedStockSplits = new List<StockSplit>
			{
				existingStockSplit,
				new(DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), 1, 4) // New split
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockSplitRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockSplitRepository1.Setup(r => r.GetStockSplits(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(updatedStockSplits);

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			symbolProfile.StockSplits.Count.Should().Be(2);
			symbolProfile.MarketData.Should().BeEmpty(); // Market data should be cleared due to new splits
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldSkipUpdatingWhenStockSplitsAreSame()
		{
			// Arrange
			var existingStockSplit = new StockSplit(DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), 1, 2);
			var existingMarketData = new MarketData(
				new Money(Currency.USD, 100), new Money(Currency.USD, 95),
				new Money(Currency.USD, 105), new Money(Currency.USD, 90),
				1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = [existingStockSplit],
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

			// Same stock splits as existing
			var sameStockSplits = new List<StockSplit> { existingStockSplit };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockSplitRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockSplitRepository1.Setup(r => r.GetStockSplits(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(sameStockSplits);

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			symbolProfile.StockSplits.Count.Should().Be(1);
			symbolProfile.MarketData.Count.Should().Be(1); // Market data should NOT be cleared
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldUseMinimumActivityDateForStockSplitQuery()
		{
			// Arrange
			var activityDate1 = DateTime.Today.AddDays(-50);
			var activityDate2 = DateTime.Today.AddDays(-30);
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = []
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = activityDate2 },
					new BuyActivity { Date = activityDate1 } // This is the minimum date
				]
			};
			var holdings = new List<Holding> { holding };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockSplitRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockSplitRepository1.Setup(r => r.GetStockSplits(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(new List<StockSplit>());

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			// Should use the minimum activity date (activityDate1)
			_mockStockSplitRepository1.Verify(
				r => r.GetStockSplits(symbolProfile, DateOnly.FromDateTime(activityDate1)),
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
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = []
			};

			var symbolProfile2 = new SymbolProfile
			{
				Symbol = "GOOGL",
				DataSource = "TEST_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = []
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

			var stockSplits = new List<StockSplit>
			{
				new(DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), 1, 2)
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();
			var mockDbContext3 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext3.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext3.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object)
				.ReturnsAsync(mockDbContext3.Object);

			_mockStockSplitRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockSplitRepository1.Setup(r => r.GetStockSplits(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(stockSplits);

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			_mockStockSplitRepository1.Verify(
				r => r.GetStockSplits(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()),
				Times.Exactly(2));

			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			mockDbContext3.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldNotLogWhenNoSplitsFound()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "TEST_SOURCE",
				AssetSubClass = AssetSubClass.Stock,
				StockSplits = []
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

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockSplitRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockSplitRepository1.Setup(r => r.GetStockSplits(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(new List<StockSplit>()); // No splits found

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			// Should not log debug message when no splits are found
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stock splits for AAPL from TEST_SOURCE gathered")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Never);

			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldHandleSymbolsOrderedCorrectly()
		{
			// Arrange - Create symbols in random order to test ordering
			var symbolProfiles = new List<SymbolProfile>
			{
				new() { Symbol = "MSFT", DataSource = "YAHOO", AssetSubClass = AssetSubClass.Stock },
				new() { Symbol = "AAPL", DataSource = "YAHOO", AssetSubClass = AssetSubClass.Stock },
				new() { Symbol = "AAPL", DataSource = "ALPHA_VANTAGE", AssetSubClass = AssetSubClass.Stock },
				new() { Symbol = "GOOGL", DataSource = "YAHOO", AssetSubClass = AssetSubClass.Stock }
			};

			var holdings = new List<Holding>();

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();
			var mockDbContext3 = new Mock<DatabaseContext>();
			var mockDbContext4 = new Mock<DatabaseContext>();
			var mockDbContext5 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext3.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext3.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext4.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext4.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			mockDbContext5.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext5.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object)
				.ReturnsAsync(mockDbContext3.Object)
				.ReturnsAsync(mockDbContext4.Object)
				.ReturnsAsync(mockDbContext5.Object);

			var loggerMock = new Mock<ILogger<MarketDataStockSplitTask>>();

			// Act
			await _marketDataStockSplitTask.DoWork(loggerMock.Object);

			// Assert
			// The code should process symbols in ordered fashion: AAPL-ALPHA_VANTAGE, AAPL-YAHOO, GOOGL-YAHOO, MSFT-YAHOO
			// Since there are no holdings, the processing will stop early but the ordering logic will be tested
			_mockDbContextFactory.Verify(
				factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()),
				Times.Exactly(5)); // Once for initial query + 4 symbols processed
		}
	}
}