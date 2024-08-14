using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using AutoFixture;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class LiabilityActivityTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
		private readonly LiabilityActivity activity;

		public LiabilityActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var dateTime = DateTime.Now;
			var unitPrice = new Money(Currency.EUR, 1);
			var transactionId = "transactionId";

			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			activity = new LiabilityActivity(account, dateTime, unitPrice, transactionId, null, null);
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
			var otherActivity = new LiabilityActivity(activity.Account, activity.Date, activity.Price, activity.TransactionId, null, null);

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
			var otherActivity = new LiabilityActivity(activity.Account, activity.Date, new Money(Currency.USD, 5), activity.TransactionId, null, null);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}
	}
}
