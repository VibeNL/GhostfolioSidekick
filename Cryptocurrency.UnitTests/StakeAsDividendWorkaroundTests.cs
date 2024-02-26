//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Symbols;

//namespace GhostfolioSidekick.Cryptocurrency.UnitTests
//{
//	public class StakeAsDividendWorkaroundTests
//	{
//		private readonly DateTime now = DateTime.UtcNow;
//		private int c = 0;
//		private readonly Fixture fixture = new();
//		private readonly SymbolProfile symbolProfileCrypto;
//		private readonly SymbolProfile symbolProfileStock;

//		public StakeAsDividendWorkaroundTests()
//		{
//			symbolProfileStock = fixture
//				.Build<SymbolProfile>()
//				.With(x => x.AssetClass, AssetClass.Equity)
//				.With(x => x.AssetSubClass, AssetSubClass.Etf)
//				.Create();
//			symbolProfileCrypto = fixture
//				.Build<SymbolProfile>()
//				.With(x => x.AssetClass, AssetClass.Cash)
//				.With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency)
//				.Create();
//		}

//		[Fact]
//		public async Task Execute_Workaround_Executed()
//		{
//			// Arrange
//			var sg = new Settings()
//			{
//				CryptoWorkaroundStakeReward = true
//			};
//			var stake = new StakeAsDividendWorkaround(sg);

//			var holding = new Holding(symbolProfileCrypto)
//			{
//				Activities = [
//					CreateDummyActivity(ActivityType.StakingReward, 42)
//				]
//			};

//			// Act
//			await stake.Execute(holding);

//			// Assert
//			holding.Activities.Should().HaveCount(2);
//			var buy = holding.Activities.Single(x => x.ActivityType == ActivityType.Buy);
//			var div = holding.Activities.Single(x => x.ActivityType == ActivityType.Dividend);

//			holding.Activities.Should().BeEquivalentTo([
//				CreateDummyActivity(ActivityType.Buy, 42, 0),
//				CreateDummyActivity(ActivityType.Dividend, 42, 0)]);
//		}

//		[Fact]
//		public async Task Execute_WorkaroundNotActivated_NotExecuted()
//		{
//			// Arrange
//			var sg = new Settings()
//			{
//				CryptoWorkaroundStakeReward = false
//			};
//			var stake = new StakeAsDividendWorkaround(sg);

//			var holding = new Holding(symbolProfileCrypto)
//			{
//				Activities = [
//					CreateDummyActivity(ActivityType.StakingReward, 42)
//				]
//			};

//			// Act
//			await stake.Execute(holding);

//			// Assert
//			holding.Activities.Should().HaveCount(1);
//			holding.Activities.Should().BeEquivalentTo([CreateDummyActivity(ActivityType.StakingReward, 42, 0)]);
//		}

//		[Fact]
//		public async Task Execute_NoStakeRewards_NotExecuted()
//		{
//			// Arrange
//			var sg = new Settings()
//			{
//				CryptoWorkaroundStakeReward = true
//			};
//			var stake = new StakeAsDividendWorkaround(sg);

//			var holding = new Holding(symbolProfileCrypto)
//			{
//				Activities = [
//					CreateDummyActivity(ActivityType.Buy, 42)
//				]
//			};

//			// Act
//			await stake.Execute(holding);

//			// Assert
//			holding.Activities.Should().HaveCount(1);
//			holding.Activities.Should().BeEquivalentTo([CreateDummyActivity(ActivityType.Buy, 42, 0)]);
//		}

//		private Activity CreateDummyActivity(ActivityType type, decimal amount, int? defaultC = null)
//		{
//			return new Activity(null!, type, now.AddMinutes(defaultC ?? c++), amount, new Money(Currency.EUR, 1), "A");
//		}
//	}
//}
