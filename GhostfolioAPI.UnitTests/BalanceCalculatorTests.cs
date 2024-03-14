using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{
	public class BalanceCalculatorTests
	{
		readonly Currency baseCurrency = Currency.USD;
		readonly Mock<IExchangeRateService> exchangeRateServiceMock;

		public BalanceCalculatorTests()
		{
			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			exchangeRateServiceMock
				.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);
		}

		[Fact]
		public async Task Calculate_WithKnownBalanceActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var knownBalanceActivity = PartialActivity.CreateKnownBalance(baseCurrency, DateTime.Now, 100);

			var activities = new List<PartialActivity>
			{
				knownBalanceActivity,
				PartialActivity.CreateBuy(baseCurrency, DateTime.Now.AddDays(-1), [], 1, 50, new Money(baseCurrency, 50), string.Empty),
				PartialActivity.CreateBuy(baseCurrency, DateTime.Now.AddDays(-2), [], -1, 25, new Money(baseCurrency, 25), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(knownBalanceActivity.Amount);
		}

		[Fact]
		public async Task Calculate_WithoutKnownBalanceActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(baseCurrency, DateTime.Now.AddDays(-1), [], 1, 50, new Money(baseCurrency, 50), string.Empty),
				PartialActivity.CreateSell(baseCurrency, DateTime.Now.AddDays(-2), [], 1, 25, new Money(baseCurrency, 25), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(-25);
		}


		[Fact]
		public async Task Calculate_WithUnsupportedActivityType_ThrowsNotSupportedException()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				new PartialActivity(PartialActivityType.Undefined, baseCurrency, new Money(baseCurrency, 0), string.Empty)
			};

			// Act & Assert
			await Assert.ThrowsAsync<NotSupportedException>(() =>
				new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities));
		}

		[Fact]
		public async Task Calculate_WithNoActivities_ReturnsZeroBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>();

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(0);
		}

		[Fact]
		public async Task Calculate_WithDividendActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				PartialActivity.CreateDividend(baseCurrency, DateTime.Now.AddDays(-1), [], 50, new Money(baseCurrency, 50), string.Empty),
				PartialActivity.CreateBuy(baseCurrency, DateTime.Now.AddDays(-2), [], -1, 25, new Money(baseCurrency, 25), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(25);
		}

		[Fact]
		public async Task Calculate_WithInterestActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				PartialActivity.CreateInterest(baseCurrency, DateTime.Now.AddDays(-1), 50, string.Empty, new Money(baseCurrency, 50), string.Empty),
				PartialActivity.CreateBuy(baseCurrency, DateTime.Now.AddDays(-2), [], -1, 25, new Money(baseCurrency, 25), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(25);
		}

		[Fact]
		public async Task Calculate_WithFeeActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				PartialActivity.CreateFee(baseCurrency, DateTime.Now.AddDays(-1), 50, new Money(baseCurrency, 50), string.Empty),
				PartialActivity.CreateBuy(baseCurrency, DateTime.Now.AddDays(-2), [], 1, 25, new Money(baseCurrency, 25), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(-75);
		}

		[Fact]
		public async Task Calculate_WithCashDepositWithdrawalActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				PartialActivity.CreateCashWithdrawal(baseCurrency, DateTime.Now.AddDays(-1), 50, new Money(baseCurrency, 50), string.Empty),
				PartialActivity.CreateBuy(baseCurrency, DateTime.Now.AddDays(-2), [], -1, 25, new Money(baseCurrency, 25), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(-75);
		}

		[Fact]
		public async Task Calculate_NoneSupported_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				new PartialActivity(PartialActivityType.StockSplit, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.StakingReward, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Gift, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Send, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Receive, baseCurrency, new Money(baseCurrency, 0), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(0);
		}
	}
}
