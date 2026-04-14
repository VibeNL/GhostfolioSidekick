using AwesomeAssertions;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class MarketDataGathererNotOwnedTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<IStockPriceRepository> _mockStockPriceRepository1;
		private readonly Mock<IStockPriceRepository> _mockStockPriceRepository2;
		private readonly IStockPriceRepository[] _stockPriceRepositories;
		private readonly MarketDataGathererNotOwnedTask _marketDataGathererTask;

		public MarketDataGathererNotOwnedTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockStockPriceRepository1 = new Mock<IStockPriceRepository>();
			_mockStockPriceRepository2 = new Mock<IStockPriceRepository>();
			_stockPriceRepositories = [_mockStockPriceRepository1.Object, _mockStockPriceRepository2.Object];
			_marketDataGathererTask = new MarketDataGathererNotOwnedTask(
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
		public void ExecutionFrequency_ShouldReturnDaily()
		{
			// Act
			var frequency = _marketDataGathererTask.ExecutionFrequency;

			// Assert
			frequency.Should().Be(TimeSpan.FromDays(1));
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
		public async Task DoWork_ShouldSkipOwnedSymbol()
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

			// No CalculatedSnapshots means default to owned
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

			var loggerMock = new Mock<ILogger<MarketDataGathererNotOwnedTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert - should skip because not-owned task does not process owned symbols
			_mockStockPriceRepository1.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping market data for AAPL from TEST_SOURCE")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldProcessNotOwnedSymbol()
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
				Id = 1,
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				]
			};
			var holdings = new List<Holding> { holding };

			// CalculatedSnapshot with Quantity = 0 means not currently owned
			var snapshot = new CalculatedSnapshot
			{
				HoldingId = 1,
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 0
			};

			var marketDataList = new List<MarketData>
			{
				new MarketData(Currency.USD, 100, 95, 105, 90, 1000, DateOnly.FromDateTime(DateTime.Today.AddDays(-29)))
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
			_mockStockPriceRepository1.Setup(r => r.GetStockMarketData(symbolProfile, It.IsAny<DateOnly>()))
				.ReturnsAsync(marketDataList);

			var loggerMock = new Mock<ILogger<MarketDataGathererNotOwnedTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert - should process because not-owned task processes not-owned symbols
			symbolProfile.MarketData.Count.Should().Be(1);
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
		public async Task DoWork_ShouldSkipSymbolWithPositiveQuantitySnapshot()
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
				Id = 1,
				SymbolProfiles = [symbolProfile],
				Activities =
				[
					new BuyActivity { Date = DateTime.Today.AddDays(-30) }
				]
			};
			var holdings = new List<Holding> { holding };

			// CalculatedSnapshot with Quantity > 0 means currently owned
			var snapshot = new CalculatedSnapshot
			{
				HoldingId = 1,
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 10
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

			var loggerMock = new Mock<ILogger<MarketDataGathererNotOwnedTask>>();

			// Act
			await _marketDataGathererTask.DoWork(loggerMock.Object);

			// Assert - should skip because not-owned task does not process owned symbols
			_mockStockPriceRepository1.Verify(r => r.GetStockMarketData(It.IsAny<SymbolProfile>(), It.IsAny<DateOnly>()), Times.Never);
		}
	}
}
