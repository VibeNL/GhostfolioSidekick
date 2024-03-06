using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using AutoFixture;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.UnitTests.Model.Activities.Types
{
	public class BuySellActivityTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
		private readonly BuySellActivity activity;

		public BuySellActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var dateTime = DateTime.Now;
			var quantity = 10m;
			var unitPrice = new Money(Currency.EUR, 1);
			var transactionId = "transactionId";

			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			activity = new BuySellActivity(account, dateTime, quantity, unitPrice, transactionId);
		}

		[Fact]
		public void ToString_ShouldReturnExpectedFormat()
		{
			// Arrange
			var expectedFormat = $"{activity.Account}_{activity.Date}";

			// Act
			var result = activity.ToString();

			// Assert
			result.Should().Be(expectedFormat);
		}

		[Fact]
		public async Task AreEqual_ShouldReturnTrue_WhenActivitiesAreEqual()
		{
			// Arrange
			var otherActivity = new BuySellActivity(activity.Account, activity.Date, activity.Quantity, activity.UnitPrice, activity.TransactionId);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task AreEqual_ShouldReturnFalse_WhenUnitPriceIsNotEqual()
		{
			// Arrange
			var otherActivity = new BuySellActivity(activity.Account, activity.Date, activity.Quantity, new Money(Currency.USD, 5), activity.TransactionId);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task AreEqual_ShouldReturnFalse_WhenQuantityIsNotEqual()
		{
			// Arrange
			var otherActivity = new BuySellActivity(activity.Account, activity.Date, 9M, activity.UnitPrice, activity.TransactionId);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}
	}
}
