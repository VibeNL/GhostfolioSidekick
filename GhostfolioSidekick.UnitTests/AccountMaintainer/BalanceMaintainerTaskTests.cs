using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.AccountMaintainer
{
	public class BalanceMaintainerTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<ICurrencyExchange> _mockExchangeRateService;
		private readonly Mock<IApplicationSettings> _mockApplicationSettings;
		private readonly BalanceMaintainerTask _balanceMaintainerTask;

		public BalanceMaintainerTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockExchangeRateService = new Mock<ICurrencyExchange>();
			_mockApplicationSettings = new Mock<IApplicationSettings>();

			// Setup default configuration
			var mockConfigurationInstance = new ConfigurationInstance
			{
				Settings = new Settings { RawCurrencies = "EUR;USD" }
			};
			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(mockConfigurationInstance);

			_balanceMaintainerTask = new BalanceMaintainerTask(_mockDbContextFactory.Object, _mockExchangeRateService.Object, _mockApplicationSettings.Object);
		}

		[Fact]
		public void Priority_ShouldReturnBalanceMaintainer()
		{
			// Act
			var priority = _balanceMaintainerTask.Priority;

			// Assert
			Assert.Equal(TaskPriority.BalanceMaintainer, priority);
		}

		[Fact]
		public async Task DoWork_ShouldUpdateBalances_WhenBalancesAreDifferent()
		{
			// Arrange
			var activities = new List<Activity>
			{
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now, Account = new Model.Accounts.Account { Id =1, SyncBalance = true }, Amount = new Money(Currency.USD,100) }
			};

			var existingBalances = new List<Model.Accounts.Balance>
			{
				new(DateOnly.FromDateTime(DateTime.Now), new Money(Currency.USD,50))
			};

			var mockDbContext = new Mock<DatabaseContext>();
			Model.Accounts.Account account = new() { Id = 1, Balance = existingBalances, SyncBalance = true };
			mockDbContext.Setup(db => db.Accounts).ReturnsDbSet(new List<Model.Accounts.Account>
			{
				account
			});

			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);

			_mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockDbContext.Object);

			var balanceCalculator = new BalanceCalculator(_mockExchangeRateService.Object);

			var loggerMock = new Mock<ILogger<BalanceMaintainerTask>>();

			// Act
			await _balanceMaintainerTask.DoWork(loggerMock.Object);

			// Assert
			mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldNotUpdateBalances_WhenBalancesAreSame()
		{
			// Arrange
			var existingBalances = new List<Model.Accounts.Balance>
			{
				new(DateOnly.FromDateTime(DateTime.Now), new Money(Currency.USD,100))
			};

			var mockDbContext = new Mock<DatabaseContext>();
			Model.Accounts.Account account = new() { Id = 1, Balance = existingBalances, SyncBalance = true };
			mockDbContext.Setup(db => db.Accounts).ReturnsDbSet(new List<Model.Accounts.Account>
			{
				account
			});

			var activities = new List<Activity>
			{
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now, Account = account, Amount = new Money(Currency.USD,100) }
			};

			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);

			_mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockDbContext.Object);

			var balanceCalculator = new BalanceCalculator(_mockExchangeRateService.Object);

			var loggerMock = new Mock<ILogger<BalanceMaintainerTask>>();

			// Act
			await _balanceMaintainerTask.DoWork(loggerMock.Object);

			// Assert
			mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldIgnoreAccountsWithSyncBalanceDisabled()
		{
			// Arrange
			var mockDbContext = new Mock<DatabaseContext>();
			Model.Accounts.Account accountWithSyncDisabled = new() { Id = 1, Name = "Account1", SyncBalance = false };
			Model.Accounts.Account accountWithSyncEnabled = new() { Id = 2, Name = "Account2", SyncBalance = true };

			mockDbContext.Setup(db => db.Accounts).ReturnsDbSet(new List<Model.Accounts.Account>
			{
				accountWithSyncDisabled,
				accountWithSyncEnabled
			});

			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(new List<Activity>());

			_mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockDbContext.Object);

			var loggerMock = new Mock<ILogger<BalanceMaintainerTask>>();

			// Act
			await _balanceMaintainerTask.DoWork(loggerMock.Object);

			// Assert
			// The task should complete successfully and only process accounts with SyncBalance = true
			// We verify this by ensuring the Activities DbSet was accessed (for the enabled account)
			mockDbContext.Verify(db => db.Activities, Times.Exactly(2));
		}
	}
}