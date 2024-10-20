//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.GhostfolioAPI.Strategies;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Activities.Types;
//using GhostfolioSidekick.Model.Symbols;

//namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
//{
//	public class AddStakeRewardsToPreviousBuyActivityTests
//	{
//		private AddStakeRewardsToPreviousBuyActivity _strategy;

//		public AddStakeRewardsToPreviousBuyActivityTests()
//		{
//			_strategy = new AddStakeRewardsToPreviousBuyActivity(new Settings { CryptoWorkaroundStakeReward = true });
//		}

//		[Fact]
//		public async Task Execute_ShouldAddStakeRewardsToPreviousBuyActivity()
//		{
//			// Arrange
//			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);
//			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

//			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
//			{
//				Activities = [buyActivity, stakeReward]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity.Quantity.Should().Be(15);
//			holding.Activities.Should().NotContain(stakeReward);
//		}

//		[Fact]
//		public async Task Execute_MultipleActivities_ShouldAddStakeRewardsToPreviousBuyActivity()
//		{
//			// Arrange
//			var buyActivity1 = new BuySellActivity(null!, DateTime.Now.AddDays(-3), 10, null, null);
//			var stakeReward1 = new StakingRewardActivity(null!, DateTime.Now.AddDays(-2), 5, null);
//			var buyActivity2 = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 4, null, null);
//			var stakeReward2 = new StakingRewardActivity(null!, DateTime.Now, 3, null);

//			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
//			{
//				Activities = [buyActivity1, stakeReward1, buyActivity2, stakeReward2]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity1.Quantity.Should().Be(15);
//			holding.Activities.Should().NotContain(stakeReward1);
//			buyActivity2.Quantity.Should().Be(7);
//			holding.Activities.Should().NotContain(stakeReward2);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotAddStakeRewardsIfNoPreviousBuyActivity()
//		{
//			// Arrange
//			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

//			var holding = new Holding(null)
//			{
//				Activities = [stakeReward]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			holding.Activities.Should().Contain(stakeReward);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotChangeQuantityIfNoStakeRewardActivity()
//		{
//			// Arrange
//			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);

//			var holding = new Holding(null)
//			{
//				Activities = [buyActivity]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity.Quantity.Should().Be(10);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotChangeActivitiesIfNotCryptoCurrency()
//		{
//			// Arrange
//			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);
//			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

//			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.PrivateEquity).Create())
//			{
//				Activities = [buyActivity, stakeReward]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity.Quantity.Should().Be(10);
//			holding.Activities.Should().Contain(stakeReward);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotChangeActivitiesIfCryptoWorkaroundStakeRewardIsFalse()
//		{
//			// Arrange
//			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, null, null);
//			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

//			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
//			{
//				Activities = [buyActivity, stakeReward],
//			};

//			_strategy = new AddStakeRewardsToPreviousBuyActivity(new Settings { CryptoWorkaroundStakeReward = false });

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity.Quantity.Should().Be(10);
//			holding.Activities.Should().Contain(stakeReward);
//		}

//		[Fact]
//		public async Task Execute_ShouldUpdateUnitPriceCorrectly_WhenUnitPriceIsNotNull()
//		{
//			// Arrange
//			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), 10, new Money(Currency.USD, 100), null);
//			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);

//			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
//			{
//				Activities = [buyActivity, stakeReward]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity.Quantity.Should().Be(15);
//			buyActivity.UnitPrice!.Amount.Should().Be(75);
//			holding.Activities.Should().NotContain(stakeReward);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotAddStakeRewardsToNewerBuyActivity()
//		{
//			// Arrange
//			var stakeReward = new StakingRewardActivity(null!, DateTime.Now.AddDays(-1), 5, null);
//			var buyActivity = new BuySellActivity(null!, DateTime.Now, 10, null, null);

//			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
//			{
//				Activities = [buyActivity, stakeReward]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity.Quantity.Should().Be(10);
//			holding.Activities.Should().Contain(stakeReward);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotAddStakeRewardsToSellActivity()
//		{
//			// Arrange
//			var stakeReward = new StakingRewardActivity(null!, DateTime.Now, 5, null);
//			var buyActivity = new BuySellActivity(null!, DateTime.Now.AddDays(-1), -10, null, null);

//			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
//			{
//				Activities = [buyActivity, stakeReward]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			buyActivity.Quantity.Should().Be(-10);
//			holding.Activities.Should().Contain(stakeReward);
//		}
//	}
//}
