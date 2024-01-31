using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
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
			var knownBalanceActivity = new Activity(null, ActivityType.KnownBalance, DateTime.Now, 100, new Money(baseCurrency, 1), null);

			var activities = new List<Activity>
			{
				knownBalanceActivity,
				new Activity(null, ActivityType.Buy, DateTime.Now.AddDays(-1), 1, new Money(baseCurrency, 50), null),
				new Activity(null, ActivityType.Sell, DateTime.Now.AddDays(-2), 1, new Money(baseCurrency, 25), null)
			};

			// Act
			var result = await BalanceCalculator.Calculate(baseCurrency, exchangeRateServiceMock.Object, activities);

			// Assert
			result.Money.Amount.Should().Be(knownBalanceActivity.Quantity);
		}

		[Fact]
		public async Task Calculate_WithoutKnownBalanceActivity_ReturnsExpectedBalance()
		{
			// Arrange
			var activities = new List<Activity>
			{
				new Activity(null, ActivityType.Buy, DateTime.Now.AddDays(-1), 1, new Money(baseCurrency, 50), null),
				new Activity(null, ActivityType.Sell, DateTime.Now.AddDays(-2), 1, new Money(baseCurrency, 75), null)
			};

			// Act
			var result = await BalanceCalculator.Calculate(baseCurrency, exchangeRateServiceMock.Object, activities);

			// Assert
			result.Money.Amount.Should().Be(25);
		}


		[Fact]
		public async Task Calculate_WithUnsupportedActivityType_ThrowsNotSupportedException()
		{
			// Arrange
			var activities = new List<Activity>
			{
				new Activity(null, (ActivityType)99, DateTime.Now, 1, new Money(baseCurrency, 100), null)
			};

			// Act & Assert
			await Assert.ThrowsAsync<NotSupportedException>(() =>
				BalanceCalculator.Calculate(baseCurrency, exchangeRateServiceMock.Object, activities));
		}

		[Fact]
		public async Task Calculate_WithNoActivities_ReturnsZeroBalance()
		{
			// Arrange
			var activities = new List<Activity>();

			// Act
			var result = await BalanceCalculator.Calculate(baseCurrency, exchangeRateServiceMock.Object, activities);

			// Assert
			result.Money.Amount.Should().Be(0);
		}
	}
}
