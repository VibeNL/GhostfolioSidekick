using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using AutoFixture;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class StakingRewardActivityTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
		private readonly StakingRewardActivity activity;

		public StakingRewardActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var dateTime = DateTime.Now;
			var quantity = 10m;
			var transactionId = "transactionId";

			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			activity = new StakingRewardActivity(account, dateTime, quantity, transactionId);
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
			var otherActivity = new StakingRewardActivity(activity.Account, activity.Date, activity.Quantity, activity.TransactionId);

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
			var otherActivity = new StakingRewardActivity(activity.Account, activity.Date, 9M, activity.TransactionId);

			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			// Act
			var result = await activity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}
	}
}
