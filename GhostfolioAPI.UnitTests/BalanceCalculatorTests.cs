using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
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
			result.Count.Should().Be(1);
			result.Single().Money.Amount.Should().Be(knownBalanceActivity.Amount);
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
			result.Count.Should().Be(2);
			result[0].Money.Amount.Should().Be(25);
			result[1].Money.Amount.Should().Be(-25);
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
		public async Task Calculate_WithNoActivities_ReturnsNoBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>();

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Should().BeEmpty();
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
			result.Count.Should().Be(2);
			result[0].Money.Amount.Should().Be(-25);
			result[1].Money.Amount.Should().Be(25);
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
			result.Count.Should().Be(2);
			result[0].Money.Amount.Should().Be(-25);
			result[1].Money.Amount.Should().Be(25);
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
			result.Count.Should().Be(2);
			result[0].Money.Amount.Should().Be(-25);
			result[1].Money.Amount.Should().Be(-75);
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
			result.Count.Should().Be(2);
			result[0].Money.Amount.Should().Be(-25);
			result[1].Money.Amount.Should().Be(-75);
		}

		[Fact]
		public async Task Calculate_AllSupported_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<PartialActivity>
			{
				new PartialActivity(PartialActivityType.Buy, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Sell, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Dividend, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Interest, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Fee, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.CashDeposit, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.CashWithdrawal, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Tax, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Valuable, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Liability, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.StockSplit, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.StakingReward, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Gift, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Send, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.Receive, baseCurrency, new Money(baseCurrency, 0), string.Empty),
				new PartialActivity(PartialActivityType.BondRepay, baseCurrency, new Money(baseCurrency, 0), string.Empty)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Count.Should().Be(11);
			result.TrueForAll(x => x.Money.Amount == 0).Should().BeTrue();
		}

		[Fact]
		public async Task Calculate_Bug286_ReturnsExpectedBalance()
		{
			// Arrange
			var dt1 = new DateTime(2024, 05, 14, 0, 0, 0, DateTimeKind.Utc);
			var dt2 = new DateTime(2024, 07, 2, 0, 0, 0, DateTimeKind.Utc);
			PartialActivity Generate(DateTime dateTime, decimal quantity, decimal price)
			{
				return PartialActivity.CreateBuy(Currency.EUR, dateTime, [], quantity, price, new Money(baseCurrency, quantity * price), Guid.NewGuid().ToString());
			}

			var activities = new List<PartialActivity>
			{
				Generate(dt1, 5.4800m, 5.17m), //BUY ADB
				Generate(dt1, 0.64m, 23.05m), //BUY DEF
				Generate(dt1, 0.250m, 49.43m), //BUY GHI
				Generate(dt1, 1.69m, 20.53m), //BUY JKL
				Generate(dt1, 0.35m, 28.28m), //BUY MNO
				Generate(dt2, -5.4800m, 5.21m), //SELL ADB
				Generate(dt2, -0.64m, 22.95m), //SELL DEF
				Generate(dt2, -0.250m, 46.97m), //SELL GHI
				Generate(dt2, -1.69m, 20.63m), //SELL JKL
				Generate(dt2, -0.35m, 29.52m), //SELL MNO
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Count.Should().Be(2);
			result[0].Money.Amount.Should().Be(-100);
			result[1].Money.Amount.Should().Be(0);
		}
	}
}
