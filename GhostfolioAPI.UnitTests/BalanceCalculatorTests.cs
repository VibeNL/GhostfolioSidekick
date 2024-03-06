using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{
	public class BalanceCalculatorTests
	{
		Currency baseCurrency = Currency.USD;
		Mock<IExchangeRateService> exchangeRateServiceMock;

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
			var knownBalanceActivity = new KnownBalanceActivity(null!, DateTime.Now, new Money(baseCurrency, 100), null);

			var activities = new List<IActivity>
			{
				knownBalanceActivity,
				new BuySellActivity(null, DateTime.Now.AddDays(-1), 1, new Money(baseCurrency, 50), null),
				new BuySellActivity(null, DateTime.Now.AddDays(-2), -1, new Money(baseCurrency, 25), null)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(knownBalanceActivity.Amount.Amount);
		}

		[Fact]
		public async Task Calculate_WithoutKnownBalanceActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<IActivity>
			{
				new BuySellActivity(null, DateTime.Now.AddDays(-1), 1, new Money(baseCurrency, 50), null),
				new BuySellActivity(null, DateTime.Now.AddDays(-2), -1, new Money(baseCurrency, 25), null)
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
			var activities = new List<IActivity>
			{
				new Mock<BaseActivity<IActivity>>().Object
			};

			// Act & Assert
			await Assert.ThrowsAsync<NotSupportedException>(() =>
				new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities));
		}

		[Fact]
		public async Task Calculate_WithNoActivities_ReturnsZeroBalance()
		{
			// Arrange
			var activities = new List<IActivity>();

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(0);
		}

		[Fact]
		public async Task Calculate_WithDividendActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<IActivity>
			{
				new DividendActivity(null, DateTime.Now.AddDays(-1), new Money(baseCurrency, 50), null),
				new BuySellActivity(null, DateTime.Now.AddDays(-2), -1, new Money(baseCurrency, 25), null)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(75);
		}

		[Fact]
		public async Task Calculate_WithInterestActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<IActivity>
			{
				new InterestActivity(null, DateTime.Now.AddDays(-1), new Money(baseCurrency, 50), null),
				new BuySellActivity(null, DateTime.Now.AddDays(-2), -1, new Money(baseCurrency, 25), null)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(75);
		}

		[Fact]
		public async Task Calculate_WithFeeActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<IActivity>
			{
				new FeeActivity(null, DateTime.Now.AddDays(-1), new Money(baseCurrency, 50), null),
				new BuySellActivity(null, DateTime.Now.AddDays(-2), 1, new Money(baseCurrency, 25), null)
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
			var activities = new List<IActivity>
			{
				new CashDepositWithdrawalActivity(null, DateTime.Now.AddDays(-1), new Money(baseCurrency, 50), null),
				new BuySellActivity(null, DateTime.Now.AddDays(-2), -1, new Money(baseCurrency, 25), null)
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(75);
		}

		[Fact]
		public async Task Calculate_NoneSupported_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<IActivity>
			{
				DefaultFixture.Create().Create<StockSplitActivity>(),
				DefaultFixture.Create().Create<StakingRewardActivity>(),
				DefaultFixture.Create().Create<GiftActivity>(),
				DefaultFixture.Create().Create<SendAndReceiveActivity>()
			};

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(0);
		}

		[Fact]
		public async Task Calculate_UnknownType_ThrowsException()
		{
			// Arrange
			var activities = new List<IActivity>
			{
				DefaultFixture.Create().Create<DummyActivity>()
			};

			// Act
			var result = () => new BalanceCalculator(exchangeRateServiceMock.Object).Calculate(baseCurrency, activities);

			// Assert
			await result.Should().ThrowAsync<NotSupportedException>();
		}

		private class DummyActivity : IActivity
		{
			public Account Account => throw new NotImplementedException();

			public DateTime Date { get; set; }

			public string? TransactionId => throw new NotImplementedException();

			public int? SortingPriority => 1;

			public string? Id => throw new NotImplementedException();

			public string? Description => throw new NotImplementedException();

			public Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity otherActivity)
			{
				throw new NotImplementedException();
			}
		}
	}
}
