//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.GhostfolioAPI.Strategies;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Activities.Types;
//using GhostfolioSidekick.Model.Symbols;

//namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
//{
//	public class NotNativeSupportedTransactionsInGhostfolioTests
//	{
//		private readonly NotNativeSupportedTransactionsInGhostfolio _strategy;

//		public NotNativeSupportedTransactionsInGhostfolioTests()
//		{
//			_strategy = new NotNativeSupportedTransactionsInGhostfolio();
//		}

//		[Fact]
//		public async Task Execute_ShouldReplaceSendAndReceiveActivitiesWithBuySellActivities()
//		{
//			// Arrange
//			var sendAndReceiveActivity = new Fixture().Create<SendAndReceiveActivity>();

//			var holding = new Holding(new Fixture().Create<SymbolProfile>())
//			{
//				Activities = [sendAndReceiveActivity]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			Assert.Empty(holding.Activities.OfType<SendAndReceiveActivity>());
//			Assert.Single(holding.Activities.OfType<BuySellActivity>());

//			var buySellActivity = holding.Activities.OfType<BuySellActivity>().First();
//			Assert.Equal(sendAndReceiveActivity.Account, buySellActivity.Account);
//			Assert.Equal(sendAndReceiveActivity.Date, buySellActivity.Date);
//			Assert.Equal(sendAndReceiveActivity.Quantity, buySellActivity.Quantity);
//			Assert.Equal(sendAndReceiveActivity.UnitPrice, buySellActivity.UnitPrice);
//			Assert.Equal(sendAndReceiveActivity.TransactionId, buySellActivity.TransactionId);
//			Assert.Equal(sendAndReceiveActivity.Description, buySellActivity.Description);
//			Assert.Equal(sendAndReceiveActivity.Fees, buySellActivity.Fees);
//			Assert.Equal(sendAndReceiveActivity.Id, buySellActivity.Id);
//			Assert.Equal(sendAndReceiveActivity.SortingPriority, buySellActivity.SortingPriority);
//		}

//		[Fact]
//		public async Task Execute_WithSymbolProfile_ShouldReplaceGiftActivitiesWithBuySellActivities()
//		{
//			// Arrange
//			var giftActivity = new Fixture().Create<GiftActivity>();

//			var holdingWithSymbolProfile = new Holding(new Fixture().Create<SymbolProfile>())
//			{
//				Activities = [giftActivity]
//			};

//			// Act
//			await _strategy.Execute(holdingWithSymbolProfile);

//			// Assert
//			Assert.Empty(holdingWithSymbolProfile.Activities.OfType<GiftActivity>());
//			Assert.Single(holdingWithSymbolProfile.Activities.OfType<BuySellActivity>());

//			var buySellActivity = holdingWithSymbolProfile.Activities.OfType<BuySellActivity>().First();
//			Assert.Equal(giftActivity.Account, buySellActivity.Account);
//			Assert.Equal(giftActivity.Date, buySellActivity.Date);
//			Assert.Equal(giftActivity.Quantity, buySellActivity.Quantity);
//			Assert.Equal(giftActivity.UnitPrice, buySellActivity.UnitPrice);
//			Assert.Equal(giftActivity.TransactionId, buySellActivity.TransactionId);
//			Assert.Equal(giftActivity.Description, buySellActivity.Description);
//			Assert.Equal(giftActivity.Id, buySellActivity.Id);
//			Assert.Equal(giftActivity.SortingPriority, buySellActivity.SortingPriority);
//		}

//		[Fact]
//		public async Task Execute_WithoutSymbolProfile_ShouldReplaceGiftActivitiesWithInterestActivities()
//		{
//			// Arrange
//			var giftActivity = new Fixture().Create<GiftActivity>();

//			var holdingWithoutSymbolProfile = new Holding(null)
//			{
//				Activities = [giftActivity]
//			};

//			// Act
//			await _strategy.Execute(holdingWithoutSymbolProfile);

//			// Assert
//			Assert.Empty(holdingWithoutSymbolProfile.Activities.OfType<GiftActivity>());
//			Assert.Single(holdingWithoutSymbolProfile.Activities.OfType<InterestActivity>());

//			var interestActivity = holdingWithoutSymbolProfile.Activities.OfType<InterestActivity>().First();
//			Assert.Equal(giftActivity.Account, interestActivity.Account);
//			Assert.Equal(giftActivity.Date, interestActivity.Date);
//			Assert.Equal(giftActivity.Quantity, interestActivity.Amount.Amount);
//			Assert.Equal(giftActivity.TransactionId, interestActivity.TransactionId);
//			Assert.Equal(giftActivity.Description, interestActivity.Description);
//			Assert.Equal(giftActivity.Id, interestActivity.Id);
//			Assert.Equal(giftActivity.SortingPriority, interestActivity.SortingPriority);
//		}

//		[Fact]
//		public async Task Execute_ShouldReplaceRepayBondToSellActivity()
//		{
//			// Arrange
//			var buyBondActivity = new Fixture()
//				.Build<BuySellActivity>()
//				.With(x => x.Quantity, 4)
//				.With(x => x.UnitPrice, new Model.Money(Model.Currency.EUR, 25))
//				.Create();
//			var repayBondActivity = new Fixture().Create<RepayBondActivity>();

//			var holding = new Holding(new Fixture().Create<SymbolProfile>())
//			{
//				Activities = [buyBondActivity, repayBondActivity]
//			};

//			// Act
//			await _strategy.Execute(holding);

//			// Assert
//			Assert.Empty(holding.Activities.OfType<RepayBondActivity>());
//			holding.Activities.Count.Should().Be(2);

//			var buySellActivity = holding.Activities.Except([buyBondActivity]).OfType<BuySellActivity>().First();
//			Assert.Equal(repayBondActivity.Account, buySellActivity.Account);
//			Assert.Equal(repayBondActivity.Date, buySellActivity.Date);
//			Assert.Equal(buyBondActivity.Quantity, -buySellActivity.Quantity);
//			Assert.Equal(repayBondActivity.TotalRepayAmount, buySellActivity.UnitPrice!.Times(-buySellActivity.Quantity));
//			Assert.Equal(repayBondActivity.TransactionId, buySellActivity.TransactionId);
//			Assert.Equal(repayBondActivity.Description, buySellActivity.Description);
//			Assert.Equal(repayBondActivity.Id, buySellActivity.Id);
//			Assert.Equal(repayBondActivity.SortingPriority, buySellActivity.SortingPriority);
//		}
//	}
//}
