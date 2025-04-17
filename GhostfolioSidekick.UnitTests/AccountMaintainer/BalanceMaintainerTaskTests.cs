using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.AccountMaintainer
{
	public class BalanceMaintainerTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<ICurrencyExchange> _mockExchangeRateService;
		private readonly BalanceMaintainerTask _balanceMaintainerTask;

		public BalanceMaintainerTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockExchangeRateService = new Mock<ICurrencyExchange>();
			_balanceMaintainerTask = new BalanceMaintainerTask(_mockDbContextFactory.Object, _mockExchangeRateService.Object);
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
			var activities = new List<Model.Activities.Activity>
			{
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now, Account = new Model.Accounts.Account { Id = 1 }, Amount = new Money(Currency.USD, 100) }
			};

			var existingBalances = new List<Model.Accounts.Balance>
			{
				new(DateOnly.FromDateTime(DateTime.Now), new Money(Currency.USD, 50))
			};

			var mockDbContext = new Mock<DatabaseContext>();
			Model.Accounts.Account account = new Model.Accounts.Account { Id = 1, Balance = existingBalances };
			mockDbContext.Setup(db => db.Accounts).ReturnsDbSet(new List<Model.Accounts.Account>
			{
				account
			});

			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);

			_mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockDbContext.Object);

			var balanceCalculator = new BalanceCalculator(_mockExchangeRateService.Object);

			// Act
			await _balanceMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldNotUpdateBalances_WhenBalancesAreSame()
		{
			// Arrange
			var existingBalances = new List<Model.Accounts.Balance>
			{
				new(DateOnly.FromDateTime(DateTime.Now), new Money(Currency.USD, 100))
			};

			var mockDbContext = new Mock<DatabaseContext>();
			Model.Accounts.Account account = new Model.Accounts.Account { Id = 1, Balance = existingBalances };
			mockDbContext.Setup(db => db.Accounts).ReturnsDbSet(new List<Model.Accounts.Account>
			{
				account
			});

			var activities = new List<Model.Activities.Activity>
			{
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now, Account = account, Amount = new Money(Currency.USD, 100) }
			};

			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);

			_mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockDbContext.Object);

			var balanceCalculator = new BalanceCalculator(_mockExchangeRateService.Object);

			// Act
			await _balanceMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldHandleDifferentBalanceScenarios()
		{
			// Arrange
			var activities = new List<Model.Activities.Activity>
			{
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now, Account = new Model.Accounts.Account { Id = 1 }, Amount = new Money(Currency.USD, 100) },
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now.AddDays(-1), Account = new Model.Accounts.Account { Id = 1 }, Amount = new Money(Currency.USD, 50) }
			};

			var existingBalances = new List<Model.Accounts.Balance>
			{
				new(DateOnly.FromDateTime(DateTime.Now), new Money(Currency.USD, 50))
			};

			var mockDbContext = new Mock<DatabaseContext>();
			Model.Accounts.Account account = new Model.Accounts.Account { Id = 1, Balance = existingBalances };
			mockDbContext.Setup(db => db.Accounts).ReturnsDbSet(new List<Model.Accounts.Account>
			{
				account
			});

			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);

			_mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockDbContext.Object);

			var balanceCalculator = new BalanceCalculator(_mockExchangeRateService.Object);

			// Act
			await _balanceMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldHandleDifferentAccountConfigurations()
		{
			// Arrange
			var activities = new List<Model.Activities.Activity>
			{
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now, Account = new Model.Accounts.Account { Id = 1 }, Amount = new Money(Currency.USD, 100) },
				new Model.Activities.Types.KnownBalanceActivity { Date = DateTime.Now, Account = new Model.Accounts.Account { Id = 2 }, Amount = new Money(Currency.EUR, 200) }
			};

			var existingBalances = new List<Model.Accounts.Balance>
			{
				new(DateOnly.FromDateTime(DateTime.Now), new Money(Currency.USD, 50)),
				new(DateOnly.FromDateTime(DateTime.Now), new Money(Currency.EUR, 150))
			};

			var mockDbContext = new Mock<DatabaseContext>();
			Model.Accounts.Account account1 = new Model.Accounts.Account { Id = 1, Balance = existingBalances.Where(b => b.Money.Currency == Currency.USD).ToList() };
			Model.Accounts.Account account2 = new Model.Accounts.Account { Id = 2, Balance = existingBalances.Where(b => b.Money.Currency == Currency.EUR).ToList() };
			mockDbContext.Setup(db => db.Accounts).ReturnsDbSet(new List<Model.Accounts.Account>
			{
				account1,
				account2
			});

			mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);

			_mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockDbContext.Object);

			var balanceCalculator = new BalanceCalculator(_mockExchangeRateService.Object);

			// Act
			await _balanceMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
		}
	}
}
