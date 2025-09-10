using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class MarketDataGathererTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<IStockPriceRepository> _mockStockPriceRepository1;
		private readonly Mock<IStockPriceRepository> _mockStockPriceRepository2;
		private readonly IStockPriceRepository[] _stockPriceRepositories;
		private readonly Mock<ILogger<MarketDataGathererTask>> _mockLogger;
		private readonly MarketDataGathererTask _marketDataGathererTask;

		public MarketDataGathererTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockStockPriceRepository1 = new Mock<IStockPriceRepository>();
			_mockStockPriceRepository2 = new Mock<IStockPriceRepository>();
			_stockPriceRepositories = [_mockStockPriceRepository1.Object, _mockStockPriceRepository2.Object];
			_mockLogger = new Mock<ILogger<MarketDataGathererTask>>();

			_marketDataGathererTask = new MarketDataGathererTask(
				_mockDbContextFactory.Object,
				_stockPriceRepositories,
				_mockLogger.Object);
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

			// Act
			await _marketDataGathererTask.DoWork();

			// Assert
			// Verify that only non-undefined symbols are processed (GOOGL should be included in the processing)
			// Since there are no holdings with activities for GOOGL, it should log "No activities found"
			_mockLogger.Verify(
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
				MarketData = new List<MarketData>()
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

			// Act
			await _marketDataGathererTask.DoWork();

			// Assert
			_mockLogger.Verify(
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
				MarketData = new List<MarketData>()
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				}
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

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("OTHER_SOURCE");
			_mockStockPriceRepository2.Setup(r => r.DataSource).Returns("ANOTHER_SOURCE");

			// Act
			await _marketDataGathererTask.DoWork();

			// Assert
			_mockStockPriceRepository1.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
			_mockStockPriceRepository2.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldSkipWhenMarketDataIsUpToDate()
		{
			// Arrange
			var symbolProfile = new SymbolProfile 
			{ 
				Symbol = "AAPL", 
				DataSource = "TEST_SOURCE", 
				AssetClass = AssetClass.Equity,
				MarketData = new List<MarketData>
				{
					new(new Money(Currency.USD, 100), new Money(Currency.USD, 95), 
						new Money(Currency.USD, 105), new Money(Currency.USD, 90), 
						1000, DateOnly.FromDateTime(DateTime.Today))
				}
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				}
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

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");

			// Act
			await _marketDataGathererTask.DoWork();

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Market data for AAPL from TEST_SOURCE is up to date")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
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
				MarketData = new List<MarketData>()
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				}
			};
			var holdings = new List<Holding> { holding };

			var marketDataList = new List<MarketData>
			{
				new(new Money(Currency.USD, 100), new Money(Currency.USD, 95), 
					new Money(Currency.USD, 105), new Money(Currency.USD, 90), 
					1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-29))),
				new(new Money(Currency.USD, 102), new Money(Currency.USD, 98), 
					new Money(Currency.USD, 107), new Money(Currency.USD, 95), 
					1200, DateOnly.FromDateTime(DateTime.Today.AddDays(-28)))
			};

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			// Act
			await _marketDataGathererTask.DoWork();

			// Assert
			symbolProfile.MarketData.Count.Should().Be(2);
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			
			_mockLogger.Verify(
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
			var existingMarketData = new MarketData(
				new Money(Currency.USD, 95), new Money(Currency.USD, 90), 
				new Money(Currency.USD, 100), new Money(Currency.USD, 85), 
				800, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var symbolProfile = new SymbolProfile 
			{ 
				Symbol = "AAPL", 
				DataSource = "TEST_SOURCE", 
				AssetClass = AssetClass.Equity,
				MarketData = new List<MarketData> { existingMarketData }
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				}
			};
			var holdings = new List<Holding> { holding };

			var updatedMarketData = new MarketData(
				new Money(Currency.USD, 100), new Money(Currency.USD, 95), 
				new Money(Currency.USD, 105), new Money(Currency.USD, 90), 
				1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var marketDataList = new List<MarketData> { updatedMarketData };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			// Act
			await _marketDataGathererTask.DoWork();

			// Assert
			symbolProfile.MarketData.Count.Should().Be(1);
			existingMarketData.Close.Amount.Should().Be(100); // Verify it was updated
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldSkipUpdatingWhenMarketDataIsSame()
		{
			// Arrange
			var existingMarketData = new MarketData(
				new Money(Currency.USD, 100), new Money(Currency.USD, 95), 
				new Money(Currency.USD, 105), new Money(Currency.USD, 90), 
				1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var symbolProfile = new SymbolProfile 
			{ 
				Symbol = "AAPL", 
				DataSource = "TEST_SOURCE", 
				AssetClass = AssetClass.Equity,
				MarketData = new List<MarketData> { existingMarketData }
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				}
			};
			var holdings = new List<Holding> { holding };

			// Same market data as existing
			var sameMarketData = new MarketData(
				new Money(Currency.USD, 100), new Money(Currency.USD, 95), 
				new Money(Currency.USD, 105), new Money(Currency.USD, 90), 
				1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

			var marketDataList = new List<MarketData> { sameMarketData };

			var mockDbContext1 = new Mock<DatabaseContext>();
			var mockDbContext2 = new Mock<DatabaseContext>();

			mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

			_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext1.Object)
				.ReturnsAsync(mockDbContext2.Object);

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			// Act
			await _marketDataGathererTask.DoWork();

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
				new Money(Currency.USD, 100), new Money(Currency.USD, 95), 
				new Money(Currency.USD, 105), new Money(Currency.USD, 90), 
				1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-40)));

			var symbolProfile = new SymbolProfile 
			{ 
				Symbol = "AAPL", 
				DataSource = "TEST_SOURCE", 
				AssetClass = AssetClass.Equity,
				MarketData = new List<MarketData> { existingMarketData }
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile };
			var holding = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = activityDate }
				}
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

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.MinDate).Returns(DateOnly.FromDateTime(DateTime.Today.AddDays(-100)));
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(new List<MarketData>());

			// Act
			await _marketDataGathererTask.DoWork();

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
				MarketData = new List<MarketData>()
			};

			var symbolProfile2 = new SymbolProfile 
			{ 
				Symbol = "GOOGL", 
				DataSource = "TEST_SOURCE", 
				AssetClass = AssetClass.Equity,
				MarketData = new List<MarketData>()
			};

			var symbolProfiles = new List<SymbolProfile> { symbolProfile1, symbolProfile2 };
			var holding1 = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile1 },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				}
			};
			var holding2 = new Holding 
			{ 
				SymbolProfiles = new List<SymbolProfile> { symbolProfile2 },
				Activities = new List<Activity> 
				{
					new BuyActivity { Date = DateTime.Today.AddDays(-25) }
				}
			};
			var holdings = new List<Holding> { holding1, holding2 };

			var marketDataList = new List<MarketData>
			{
				new(new Money(Currency.USD, 100), new Money(Currency.USD, 95), 
					new Money(Currency.USD, 105), new Money(Currency.USD, 90), 
					1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-29)))
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

			_mockStockPriceRepository1.Setup(r => r.DataSource).Returns("TEST_SOURCE");
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			// Act
			await _marketDataGathererTask.DoWork();

			// Assert
			_mockStockPriceRepository1.Verify(
				r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), 
				Times.Exactly(2));
			
			mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			mockDbContext3.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}