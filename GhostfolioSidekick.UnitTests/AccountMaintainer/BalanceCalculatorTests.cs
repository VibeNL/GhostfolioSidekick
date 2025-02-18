using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Moq;
using Shouldly;
using GhostfolioSidekick.Database.Repository;

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
			result.ShouldHaveSingleItem();
			result.First().Money.Amount.ShouldBe(100);
			result.First().Money.Currency.ShouldBe(baseCurrency);
		}

		[Fact]
		public async Task Calculate_ShouldReturnCalculatedBalances_WhenNoKnownBalanceActivitiesExist()
		{
			// Arrange
			var baseCurrency = Currency.USD;
			var buySellActivity = new BuySellActivity
			{
				Date = DateTime.UtcNow,
				TotalTransactionAmount = new Money(baseCurrency, 50),
				Quantity = 1
			};
			var cashDepositActivity = new CashDepositWithdrawalActivity
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
			result.ShouldHaveCount(2);
			result.First().Money.Amount.ShouldBe(100);
			result.Last().Money.Amount.ShouldBe(50);
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
			await act.ShouldThrowAsync<NotImplementedException>()
				.WithMessage("Activity type UnknownActivity is not implemented.");
		}

		private record UnknownActivity : Activity
		{
			public UnknownActivity()
			{
				Date = DateTime.UtcNow;
			}
		}
	}
}
