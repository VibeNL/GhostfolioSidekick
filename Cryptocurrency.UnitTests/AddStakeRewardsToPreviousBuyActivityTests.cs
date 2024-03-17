using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Cryptocurrency.UnitTests
{
	public class AddStakeRewardsToPreviousBuyActivityTests
	{
		private AddStakeRewardsToPreviousBuyActivity _strategy;

		public AddStakeRewardsToPreviousBuyActivityTests()
		{
			_strategy = new AddStakeRewardsToPreviousBuyActivity(new Settings { CryptoWorkaroundStakeReward = true });
		}

		[Fact]
		public async Task Execute_ShouldAddStakeRewardsToPreviousBuyActivity()
		{
			// Arrange
			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);
			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
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

			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
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

		[Fact]
		public async Task Execute_ShouldNotChangeQuantityIfNoStakeRewardActivity()
		{
			// Arrange
			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);

			var holding = new Holding(null)
			{
				Activities = new List<IActivity> { buyActivity }
			};

			// Act
			await _strategy.Execute(holding);

			// Assert
			buyActivity.Quantity.Should().Be(10);
		}

		[Fact]
		public async Task Execute_ShouldNotChangeActivitiesIfNotCryptoCurrency()
		{
			// Arrange
			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);
			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.PrivateEquity).Create())
			{
				Activities = new List<IActivity> { buyActivity, stakeReward }
			};

			// Act
			await _strategy.Execute(holding);

			// Assert
			buyActivity.Quantity.Should().Be(10);
			holding.Activities.Should().Contain(stakeReward);
		}

		[Fact]
		public async Task Execute_ShouldNotChangeActivitiesIfCryptoWorkaroundDustIsFalse()
		{
			// Arrange
			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);
			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = new List<IActivity> { buyActivity, stakeReward },
			};

			_strategy = new AddStakeRewardsToPreviousBuyActivity(new Settings { CryptoWorkaroundDust = false });

			// Act
			await _strategy.Execute(holding);

			// Assert
			buyActivity.Quantity.Should().Be(10);
			holding.Activities.Should().Contain(stakeReward);
		}

	}
}
