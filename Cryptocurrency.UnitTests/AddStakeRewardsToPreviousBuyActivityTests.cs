using FluentAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.Cryptocurrency.UnitTests
{
	public class AddStakeRewardsToPreviousBuyActivityTests
	{
		private readonly AddStakeRewardsToPreviousBuyActivity _strategy;

		public AddStakeRewardsToPreviousBuyActivityTests()
		{
			_strategy = new AddStakeRewardsToPreviousBuyActivity();
		}

		[Fact]
		public async Task Execute_ShouldAddStakeRewardsToPreviousBuyActivity()
		{
			// Arrange
			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);
			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

			var holding = new Holding(null)
			{
				Activities = new List<IActivity> { buyActivity, stakeReward }
			};

			// Act
			await _strategy.Execute(holding);

			// Assert
			buyActivity.Quantity.Should().Be(15);
			holding.Activities.Should().NotContain(stakeReward);
		}

		[Fact]
		public async Task Execute_MultipleActivities_ShouldAddStakeRewardsToPreviousBuyActivity()
		{
			// Arrange
			var buyActivity1 = new BuySellActivity(null!, DateTime.Now.AddDays(-3), 10, null, null);
			var stakeReward1 = new StakingRewardActivity(null!, DateTime.Now.AddDays(-2), 5, null);
			var buyActivity2 = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 4, null, null);
			var stakeReward2 = new StakingRewardActivity(null!, DateTime.Now, 3, null);

			var holding = new Holding(null)
			{
				Activities = new List<IActivity> { buyActivity1, stakeReward1, buyActivity2, stakeReward2 }
			};

			// Act
			await _strategy.Execute(holding);

			// Assert
			buyActivity1.Quantity.Should().Be(15);
			holding.Activities.Should().NotContain(stakeReward1);
			buyActivity2.Quantity.Should().Be(7);
			holding.Activities.Should().NotContain(stakeReward2);
		}

		[Fact]
		public async Task Execute_ShouldNotAddStakeRewardsIfNoPreviousBuyActivity()
		{
			// Arrange
			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

			var holding = new Holding(null)
			{
				Activities = new List<IActivity> { stakeReward }
			};

			// Act
			await _strategy.Execute(holding);

			// Assert
			holding.Activities.Should().Contain(stakeReward);
		}
	}
}
