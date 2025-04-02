using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Moq;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.ProcessingService.Sync;

namespace GhostfolioSidekick.ProcessingService.UnitTests.Sync
{
	public class SyncManualSymbolsWithGhostfolioTaskTests : IDisposable
	{
		private readonly DbContextOptions<DatabaseContext> _dbContextOptions;
		private readonly Mock<IGhostfolioSync> _mockGhostfolioSync;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly string _databaseFilePath;

		public SyncManualSymbolsWithGhostfolioTaskTests()
		{
			_databaseFilePath = $"test_ghostfoliosidekick_{Guid.NewGuid()}.db";

			_dbContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite($"Data Source={_databaseFilePath}")
			.Options;
			_mockGhostfolioSync = new Mock<IGhostfolioSync>();
			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
		}

		private DatabaseContext CreateTask(out SyncManualSymbolsWithGhostfolioTask task)
		{
			var context = new DatabaseContext(_dbContextOptions);
			context.Database.EnsureCreated();

			task = new SyncManualSymbolsWithGhostfolioTask(
				new DbContextFactory(context),
				_mockGhostfolioSync.Object,
				_mockCurrencyExchange.Object);

			return context;
		}

		[Fact]
		public async Task DoWork_ShouldSyncSymbolProfilesAndMarketData()
		{
			// Arrange
			using var context = CreateTask(out SyncManualSymbolsWithGhostfolioTask task);
			var symbol = new SymbolProfile
			{
				Symbol = "W",
				DataSource = Datasource.MANUAL,
				Currency = Currency.USD,
				SectorWeights = new List<SectorWeight>(),
				CountryWeight = new List<CountryWeight>()
			};
			var holding = new Holding
			{
				SymbolProfiles = [symbol],
				Activities = [new BuySellActivity {
						Date = DateTime.Today.AddDays(-100),
						UnitPrice = new Money(Currency.USD, 100),
						TransactionId = "A",
				Account = new Model.Accounts.Account{ Name = "DS" } }            ]
			};

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			_mockCurrencyExchange.Setup(exchange => exchange.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency curr, DateOnly date) => money);

			// Act
			await task.DoWork();

			// Assert
			_mockGhostfolioSync.Verify(sync => sync.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>()), Times.Once);
			_mockGhostfolioSync.Verify(sync => sync.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldHandleEmptySymbolProfiles()
		{
			// Arrange
			using var context = CreateTask(out SyncManualSymbolsWithGhostfolioTask task);

			// Act
			await task.DoWork();

			// Assert
			_mockGhostfolioSync.Verify(sync => sync.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>()), Times.Once);
			_mockGhostfolioSync.Verify(sync => sync.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldHandleEmptyActivities()
		{
			// Arrange
			using var context = CreateTask(out SyncManualSymbolsWithGhostfolioTask task);
			var symbol = new SymbolProfile
			{
				Symbol = "W",
				DataSource = Datasource.MANUAL,
				Currency = Currency.USD,
				SectorWeights = new List<SectorWeight>(),
				CountryWeight = new List<CountryWeight>()
			};
			context.SymbolProfiles.Add(symbol);
			await context.SaveChangesAsync();

			// Act
			await task.DoWork();

			// Assert
			_mockGhostfolioSync.Verify(sync => sync.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>()), Times.Once);
			_mockGhostfolioSync.Verify(sync => sync.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>()), Times.Once);
		}

		public void Dispose()
		{
			try
			{
				if (File.Exists(_databaseFilePath))
				{
					File.Delete(_databaseFilePath);
				}
			}
			catch (Exception)
			{
				// Ignore
			}
		}

		private class DbContextFactory(DatabaseContext context) : IDbContextFactory<DatabaseContext>
		{
			public DatabaseContext CreateDbContext()
			{
				return context;
			}
		}
	}
}
