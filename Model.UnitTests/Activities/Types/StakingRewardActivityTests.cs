using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class StakingRewardActivityTests
	{
		private readonly StakingRewardActivity activity;

		public StakingRewardActivityTests()
		{
			var account = CustomFixture.New().Create<Account>();
			var dateTime = DateTime.Now;
			var quantity = 10m;
			var transactionId = "transactionId";

			activity = new StakingRewardActivity(account, new Holding(), [], dateTime, quantity, transactionId, null, null);
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
	}
}
