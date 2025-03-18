using GhostfolioSidekick.Sync;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Moq;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.Sync
{
	public class SyncManualSymbolsWithGhostfolioTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<IGhostfolioSync> _mockGhostfolioSync;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly SyncManualSymbolsWithGhostfolioTask _task;

		public SyncManualSymbolsWithGhostfolioTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockGhostfolioSync = new Mock<IGhostfolioSync>();
			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_task = new SyncManualSymbolsWithGhostfolioTask(
				_mockDbContextFactory.Object,
				_mockGhostfolioSync.Object,
				_mockCurrencyExchange.Object);
		}

		[Fact]
		public async Task DoWork_ShouldSyncSymbolProfilesAndMarketData()
		{
			// Arrange
			var mockDbContext = new Mock<DatabaseContext>();
			var symbolProfiles = new List<SymbolProfile>
		{
			new SymbolProfile { DataSource = Datasource.MANUAL }
		};
			mockDbContext.Setup(db => db.SymbolProfiles)
				.ReturnsDbSet(symbolProfiles.AsQueryable());
			_mockDbContextFactory.Setup(factory => factory.CreateDbContext())
				.Returns(mockDbContext.Object);

			var activities = new List<BuySellActivity>
		{
			new BuySellActivity { Date = DateTime.Today.AddDays(-1), UnitPrice = new Money(Currency.USD, 100) }
		};
			mockDbContext.Setup(db => db.Activities)
				.ReturnsDbSet(activities.AsQueryable());

			_mockCurrencyExchange.Setup(exchange => exchange.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency curr, DateOnly date) => money);

			// Act
			await _task.DoWork();

			// Assert
			_mockGhostfolioSync.Verify(sync => sync.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>()), Times.Once);
			_mockGhostfolioSync.Verify(sync => sync.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldHandleEmptySymbolProfiles()
		{
			// Arrange
			var mockDbContext = new Mock<DatabaseContext>();
			var symbolProfiles = new List<SymbolProfile>();
			mockDbContext.Setup(db => db.SymbolProfiles)
				.ReturnsDbSet(symbolProfiles.AsQueryable());
			_mockDbContextFactory.Setup(factory => factory.CreateDbContext())
				.Returns(mockDbContext.Object);

			// Act
			await _task.DoWork();

			// Assert
			_mockGhostfolioSync.Verify(sync => sync.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>()), Times.Once);
			_mockGhostfolioSync.Verify(sync => sync.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldHandleEmptyActivities()
		{
			// Arrange
			var mockDbContext = new Mock<DatabaseContext>();
			var symbolProfiles = new List<SymbolProfile>
		{
			new SymbolProfile { DataSource = Datasource.MANUAL }
		};
			mockDbContext.Setup(db => db.SymbolProfiles)
				.ReturnsDbSet(symbolProfiles.AsQueryable());
			_mockDbContextFactory.Setup(factory => factory.CreateDbContext())
				.Returns(mockDbContext.Object);

			var activities = new List<BuySellActivity>();
			mockDbContext.Setup(db => db.Activities)
				.ReturnsDbSet(activities.AsQueryable());

			// Act
			await _task.DoWork();

			// Assert
			_mockGhostfolioSync.Verify(sync => sync.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>()), Times.Once);
			_mockGhostfolioSync.Verify(sync => sync.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>()), Times.Once);
		}
	}
}