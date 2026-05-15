using AwesomeAssertions;
using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.UnitTests.AccountMaintainer
{
	public class BalanceCalculatorTests
	{
		private readonly Mock<ICurrencyExchange> mockExchangeRateService;
		private readonly BalanceCalculator balanceCalculator;

		public BalanceCalculatorTests()
		{
			mockExchangeRateService = new Mock<ICurrencyExchange>();
			balanceCalculator = new BalanceCalculator(mockExchangeRateService.Object);
		}

		[Fact]
		public async Task Calculate_ShouldReturnKnownBalances_WhenKnownBalanceActivitiesExist()
		{
			// Arrange
			Currency baseCurrency = Currency.USD;
			KnownBalanceActivity knownBalanceActivity = new()
			{
				Date = DateTime.UtcNow,
				Amount = new Money(baseCurrency, 100)
			};
			List<Activity> activities = new()
			{ knownBalanceActivity };

			// Act
			List<Balance> result = await balanceCalculator.Calculate(baseCurrency, activities);

			// Assert
			_ = result.Should().HaveCount(1);
			_ = result[0].Money.Amount.Should().Be(100);
			_ = result[0].Money.Currency.Should().Be(baseCurrency);
		}

		[Fact]
		public async Task Calculate_ShouldReturnCalculatedBalances_WhenNoKnownBalanceActivitiesExist()
		{
			// Arrange
			Currency baseCurrency = Currency.USD;
			BuyActivity buySellActivity = new()
			{
				Date = DateTime.UtcNow,
				Quantity = 1
			};
			CashDepositActivity cashDepositActivity = new()
			{
				Date = DateTime.UtcNow.AddDays(-1),
				Amount = new Money(baseCurrency, 100)
			};
			List<Activity> activities = new()
			{ buySellActivity, cashDepositActivity };

			_ = mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => money);

			// Act
			List<Balance> result = await balanceCalculator.Calculate(baseCurrency, activities);

			// Assert
			_ = result.Should().HaveCount(2);
			_ = result[0].Money.Amount.Should().Be(100);
			_ = result[1].Money.Amount.Should().Be(50);
		}

		[Fact]
		public async Task Calculate_ShouldThrowNotImplementedException_ForUnknownActivityType()
		{
			// Arrange
			Currency baseCurrency = Currency.USD;
			UnknownActivity unknownActivity = new()
			{
				Date = DateTime.UtcNow
			};
			List<Activity> activities = new()
			{ unknownActivity };

			// Act
			Func<Task> act = async () => await balanceCalculator.Calculate(baseCurrency, activities);

			// Assert
			_ = await act.Should().ThrowAsync<NotImplementedException>()
				.WithMessage("Activity type UnknownActivity is not implemented.");
		}

		private record UnknownActivity : Activity
		{
			public UnknownActivity()
			{
				Date = DateTime.UtcNow;
			}
		}

		[Fact]
		public async Task Calculate_ShouldHandleAllSupportedActivityTypes()
		{
			Currency baseCurrency = Currency.USD;
			DateTime date = DateTime.UtcNow.Date;
			List<Activity> activities = new()
			{
				new BuyActivity { Date = date, UnitPrice = new Money(baseCurrency, 10), Quantity = 1 },
				new SellActivity { Date = date.AddDays(1), UnitPrice = new Money(baseCurrency, 20), Quantity = 1 },
				new CashDepositActivity { Date = date.AddDays(2), Amount = new Money(baseCurrency, 30) },
				new CashWithdrawalActivity { Date = date.AddDays(3), Amount = new Money(baseCurrency, 5) },
				new DividendActivity { Date = date.AddDays(4), Amount = new Money(baseCurrency, 7) },
				new FeeActivity { Date = date.AddDays(5), Amount = new Money(baseCurrency, 2) },
				new InterestActivity { Date = date.AddDays(6), Amount = new Money(baseCurrency, 3) },
				new RepayBondActivity { Date = date.AddDays(7), Amount = new Money(baseCurrency, 4) },
				new GiftFiatActivity { Date = date.AddDays(8), Amount = new Money(baseCurrency, 6) },
				new CorrectionActivity { Date = date.AddDays(9), Amount = new Money(baseCurrency, 1) },
			};

			_ = mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			List<Balance> result = await balanceCalculator.Calculate(baseCurrency, activities);

			_ = result.Should().NotBeNull();
			_ = result.Select(b => b.Money.Currency).Should().AllBeEquivalentTo(baseCurrency);
			_ = result.Count.Should().Be(10);
			_ = result.Any(b => b.Money.Amount < 0).Should().BeTrue();
		}

		[Fact]
		public async Task Calculate_ShouldIgnoreActivitiesWithNoBalanceImpact()
		{
			Currency baseCurrency = Currency.USD;
			DateTime date = DateTime.UtcNow.Date;
			List<Activity> activities = new()
			{
				new BuyActivity { Date = date, UnitPrice = new Money(baseCurrency, 10), Quantity = 1 },
				new GhostfolioSidekick.Model.Activities.Types.GiftAssetActivity { Date = date.AddDays(1) },
				new GhostfolioSidekick.Model.Activities.Types.LiabilityActivity { Date = date.AddDays(2) },
				new GhostfolioSidekick.Model.Activities.Types.ReceiveActivity { Date = date.AddDays(3) },
				new GhostfolioSidekick.Model.Activities.Types.SendActivity { Date = date.AddDays(4) },
				new GhostfolioSidekick.Model.Activities.Types.StakingRewardActivity { Date = date.AddDays(5) },
				new GhostfolioSidekick.Model.Activities.Types.ValuableActivity { Date = date.AddDays(6) },
			};

			_ = mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			List<Balance> result = await balanceCalculator.Calculate(baseCurrency, activities);

			_ = result.Should().NotBeNull();
			_ = result.Count.Should().Be(1);
			_ = result[0].Money.Amount.Should().Be(-10);
		}

		[Fact]
		public async Task Calculate_ShouldGroupActivitiesByDate()
		{
			Currency baseCurrency = Currency.USD;
			DateTime date = DateTime.UtcNow.Date;
			List<Activity> activities = new()
			{
				new BuyActivity { Date = date, UnitPrice = new Money(baseCurrency, 10), Quantity = 1 },
				new CashDepositActivity { Date = date, Amount = new Money(baseCurrency, 20) },
				new FeeActivity { Date = date, Amount = new Money(baseCurrency, 2) },
			};

			_ = mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			List<Balance> result = await balanceCalculator.Calculate(baseCurrency, activities);

			_ = result.Should().NotBeNull();
			_ = result.Count.Should().Be(1);
			_ = result[0].Money.Amount.Should().Be(8); // 20 - 10 - 2
		}

		[Fact]
		public async Task Calculate_ShouldApplyCurrencyConversion()
		{
			Currency baseCurrency = Currency.EUR;
			DateTime date = DateTime.UtcNow.Date;
			List<Activity> activities = new()
			{
				new BuyActivity { Date = date, UnitPrice = new Money(Currency.USD, 10), Quantity = 1 },
				new CashDepositActivity { Date = date, Amount = new Money(Currency.USD, 20) },
			};

			_ = mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => new Money(currency, money.Amount * 2));

			List<Balance> result = await balanceCalculator.Calculate(baseCurrency, activities);

			_ = result.Should().NotBeNull();
			_ = result.Count.Should().Be(1);
			_ = result[0].Money.Currency.Should().Be(baseCurrency);
			_ = result[0].Money.Amount.Should().Be(20); // (20 - 10) * 2
		}

		[Fact]
		public async Task Calculate_ShouldHandleNegativeAndZeroAmounts()
		{
			Currency baseCurrency = Currency.USD;
			DateTime date = DateTime.UtcNow.Date;
			List<Activity> activities = new()
			{
				new DividendActivity { Date = date, Amount = new Money(baseCurrency, -5) },
				new FeeActivity { Date = date, Amount = new Money(baseCurrency, 0) },
			};

			_ = mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			List<Balance> result = await balanceCalculator.Calculate(baseCurrency, activities);

			_ = result.Should().NotBeNull();
			_ = result.Count.Should().Be(1);
			_ = result[0].Money.Amount.Should().Be(-5);
		}
	}
}