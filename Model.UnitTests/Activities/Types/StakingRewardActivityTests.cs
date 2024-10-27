using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using AutoFixture;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class StakingRewardActivityTests
	{
		private readonly StakingRewardActivity activity;

		public StakingRewardActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var symbolProfile = new Fixture().Create<SymbolProfile>();
			var dateTime = DateTime.Now;
			var quantity = 10m;
			var transactionId = "transactionId";

			activity = new StakingRewardActivity(symbolProfile, account, [], dateTime, quantity, transactionId, null, null);
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
