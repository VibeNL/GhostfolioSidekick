using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using AutoFixture;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class DividendActivityTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
		private readonly DividendActivity activity;

		public DividendActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var dateTime = DateTime.Now;
			var amount = new Money(Currency.EUR, 1);
			var transactionId = "transactionId";

			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			activity = new DividendActivity(account, dateTime, amount, transactionId);
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
			var otherActivity = new DividendActivity(activity.Account, activity.Date, activity.Amount, activity.TransactionId);

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
			var otherActivity = new DividendActivity(activity.Account, activity.Date, new Money(Currency.USD, 5), activity.TransactionId);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}
	}
}
