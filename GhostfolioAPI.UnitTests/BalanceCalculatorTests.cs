using FluentAssertions;
using GhostfolioSidekick.Model;
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
		ILogger logger = new Mock<ILogger>().Object;

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
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object, logger).Calculate(baseCurrency, activities);

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
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object, logger).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(25);
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
				new BalanceCalculator(exchangeRateServiceMock.Object, logger).Calculate(baseCurrency, activities));
		}

		[Fact]
		public async Task Calculate_WithNoActivities_ReturnsZeroBalance()
		{
			// Arrange
			var activities = new List<IActivity>();

			// Act
			var result = await new BalanceCalculator(exchangeRateServiceMock.Object, logger).Calculate(baseCurrency, activities);

			// Assert
			result.Money.Amount.Should().Be(0);
		}
	}
}
