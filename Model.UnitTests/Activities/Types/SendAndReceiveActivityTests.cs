using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using AutoFixture;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class SendAndReceiveActivityTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
		private readonly SendAndReceiveActivity activity;

		public SendAndReceiveActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var dateTime = DateTime.Now;
			var quantity = 10m;
			var transactionId = "transactionId";

			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			activity = new SendAndReceiveActivity(account, dateTime, quantity, transactionId);
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
			var otherActivity = new SendAndReceiveActivity(activity.Account, activity.Date, activity.Quantity, activity.TransactionId);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task AreEqual_ShouldReturnFalse_WhenQuantityIsNotEqual()
		{
			// Arrange
			var otherActivity = new SendAndReceiveActivity(activity.Account, activity.Date, 9M, activity.TransactionId);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task AreEqual_ShouldReturnFalseWithUnitPrice_WhenQuantityIsNotEqual()
		{
			// Arrange
			var otherActivity = new SendAndReceiveActivity(activity.Account, activity.Date, 9M, activity.TransactionId)
			{
				UnitPrice = new Money(Currency.USD, 10M)
			};

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}
	}
}
