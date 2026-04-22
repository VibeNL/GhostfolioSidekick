using AwesomeAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
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
			var baseCurrency = Currency.USD;
			var knownBalanceActivity = new KnownBalanceActivity
			{
				Date = DateTime.UtcNow,
				Amount = new Money(baseCurrency, 100)
			};
			var activities = new List<Activity> { knownBalanceActivity };

			// Act
			var result = await balanceCalculator.Calculate(baseCurrency, activities);

			// Assert
			result.Should().HaveCount(1);
			result[0].Money.Amount.Should().Be(100);
			result[0].Money.Currency.Should().Be(baseCurrency);
		}

		[Fact]
		public async Task Calculate_ShouldReturnCalculatedBalances_WhenNoKnownBalanceActivitiesExist()
		{
			// Arrange
			var baseCurrency = Currency.USD;
			var buySellActivity = new BuyActivity
			{
				Date = DateTime.UtcNow,
				TotalTransactionAmount = new Money(baseCurrency, 50),
				Quantity = 1
			};
			var cashDepositActivity = new CashDepositActivity
			{
				Date = DateTime.UtcNow.AddDays(-1),
				Amount = new Money(baseCurrency, 100)
			};
			var activities = new List<Activity> { buySellActivity, cashDepositActivity };

			mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => money);

			// Act
			var result = await balanceCalculator.Calculate(baseCurrency, activities);

			// Assert
			result.Should().HaveCount(2);
			result[0].Money.Amount.Should().Be(100);
			result[1].Money.Amount.Should().Be(50);
		}

		[Fact]
		public async Task Calculate_ShouldThrowNotImplementedException_ForUnknownActivityType()
		{
			// Arrange
			var baseCurrency = Currency.USD;
			var unknownActivity = new UnknownActivity
			{
				Date = DateTime.UtcNow
			};
			var activities = new List<Activity> { unknownActivity };

			// Act
			Func<Task> act = async () => await balanceCalculator.Calculate(baseCurrency, activities);

			// Assert
			await act.Should().ThrowAsync<NotImplementedException>()
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
			var baseCurrency = Currency.USD;
			var date = DateTime.UtcNow.Date;
			var activities = new List<Activity>
			{
				new BuyActivity { Date = date, TotalTransactionAmount = new Money(baseCurrency, 10), Quantity = 1 },
				new SellActivity { Date = date.AddDays(1), TotalTransactionAmount = new Money(baseCurrency, 20), Quantity = 1 },
				new CashDepositActivity { Date = date.AddDays(2), Amount = new Money(baseCurrency, 30) },
				new CashWithdrawalActivity { Date = date.AddDays(3), Amount = new Money(baseCurrency, 5) },
				new DividendActivity { Date = date.AddDays(4), Amount = new Money(baseCurrency, 7) },
				new FeeActivity { Date = date.AddDays(5), Amount = new Money(baseCurrency, 2) },
				new InterestActivity { Date = date.AddDays(6), Amount = new Money(baseCurrency, 3) },
				new RepayBondActivity { Date = date.AddDays(7), Amount = new Money(baseCurrency, 4) },
				new GiftFiatActivity { Date = date.AddDays(8), Amount = new Money(baseCurrency, 6) },
				new CorrectionActivity { Date = date.AddDays(9), Amount = new Money(baseCurrency, 1) },
			};

			mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			var result = await balanceCalculator.Calculate(baseCurrency, activities);

			result.Should().NotBeNull();
			result.Select(b => b.Money.Currency).Should().AllBeEquivalentTo(baseCurrency);
			result.Count.Should().Be(10);
			result.Any(b => b.Money.Amount < 0).Should().BeTrue();
		}

		[Fact]
		public async Task Calculate_ShouldIgnoreActivitiesWithNoBalanceImpact()
		{
			var baseCurrency = Currency.USD;
			var date = DateTime.UtcNow.Date;
			var activities = new List<Activity>
			{
				new BuyActivity { Date = date, TotalTransactionAmount = new Money(baseCurrency, 10), Quantity = 1 },
				new GhostfolioSidekick.Model.Activities.Types.GiftAssetActivity { Date = date.AddDays(1) },
				new GhostfolioSidekick.Model.Activities.Types.LiabilityActivity { Date = date.AddDays(2) },
				new GhostfolioSidekick.Model.Activities.Types.ReceiveActivity { Date = date.AddDays(3) },
				new GhostfolioSidekick.Model.Activities.Types.SendActivity { Date = date.AddDays(4) },
				new GhostfolioSidekick.Model.Activities.Types.StakingRewardActivity { Date = date.AddDays(5) },
				new GhostfolioSidekick.Model.Activities.Types.ValuableActivity { Date = date.AddDays(6) },
			};

			mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			var result = await balanceCalculator.Calculate(baseCurrency, activities);

			result.Should().NotBeNull();
			result.Count.Should().Be(1);
			result[0].Money.Amount.Should().Be(-10);
		}

		[Fact]
		public async Task Calculate_ShouldGroupActivitiesByDate()
		{
			var baseCurrency = Currency.USD;
			var date = DateTime.UtcNow.Date;
			var activities = new List<Activity>
			{
				new BuyActivity { Date = date, TotalTransactionAmount = new Money(baseCurrency, 10), Quantity = 1 },
				new CashDepositActivity { Date = date, Amount = new Money(baseCurrency, 20) },
				new FeeActivity { Date = date, Amount = new Money(baseCurrency, 2) },
			};

			mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			var result = await balanceCalculator.Calculate(baseCurrency, activities);

			result.Should().NotBeNull();
			result.Count.Should().Be(1);
			result[0].Money.Amount.Should().Be(8); // 20 - 10 - 2
		}

		[Fact]
		public async Task Calculate_ShouldApplyCurrencyConversion()
		{
			var baseCurrency = Currency.EUR;
			var date = DateTime.UtcNow.Date;
			var activities = new List<Activity>
			{
				new BuyActivity { Date = date, TotalTransactionAmount = new Money(Currency.USD, 10), Quantity = 1 },
				new CashDepositActivity { Date = date, Amount = new Money(Currency.USD, 20) },
			};

			mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => new Money(currency, money.Amount * 2));

			var result = await balanceCalculator.Calculate(baseCurrency, activities);

			result.Should().NotBeNull();
			result.Count.Should().Be(1);
			result[0].Money.Currency.Should().Be(baseCurrency);
			result[0].Money.Amount.Should().Be(20); // (20 - 10) * 2
		}

		[Fact]
		public async Task Calculate_ShouldHandleNegativeAndZeroAmounts()
		{
			var baseCurrency = Currency.USD;
			var date = DateTime.UtcNow.Date;
			var activities = new List<Activity>
			{
				new DividendActivity { Date = date, Amount = new Money(baseCurrency, -5) },
				new FeeActivity { Date = date, Amount = new Money(baseCurrency, 0) },
			};

			mockExchangeRateService
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), baseCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly d) => money);

			var result = await balanceCalculator.Calculate(baseCurrency, activities);

			result.Should().NotBeNull();
			result.Count.Should().Be(1);
			result[0].Money.Amount.Should().Be(-5);
		}
	}
}