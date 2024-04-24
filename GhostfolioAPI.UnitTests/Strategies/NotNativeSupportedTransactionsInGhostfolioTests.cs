using AutoFixture;
using GhostfolioSidekick.GhostfolioAPI.Strategies;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
{
	public class NotNativeSupportedTransactionsInGhostfolioTests
	{
		private readonly NotNativeSupportedTransactionsInGhostfolio _strategy;

		public NotNativeSupportedTransactionsInGhostfolioTests()
		{
			_strategy = new NotNativeSupportedTransactionsInGhostfolio();
		}

		[Fact]
		public async Task Execute_ShouldReplaceSendAndReceiveActivitiesWithBuySellActivities()
		{
			// Arrange
			var sendAndReceiveActivity = new Fixture().Create<SendAndReceiveActivity>();

			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = [sendAndReceiveActivity]
			};

			// Act
			await _strategy.Execute(holding);

			// Assert
			Assert.Empty(holding.Activities.OfType<SendAndReceiveActivity>());
			Assert.Single(holding.Activities.OfType<BuySellActivity>());

			var buySellActivity = holding.Activities.OfType<BuySellActivity>().First();
			Assert.Equal(sendAndReceiveActivity.Account, buySellActivity.Account);
			Assert.Equal(sendAndReceiveActivity.Date, buySellActivity.Date);
			Assert.Equal(sendAndReceiveActivity.Quantity, buySellActivity.Quantity);
			Assert.Equal(sendAndReceiveActivity.UnitPrice, buySellActivity.UnitPrice);
			Assert.Equal(sendAndReceiveActivity.TransactionId, buySellActivity.TransactionId);
			Assert.Equal(sendAndReceiveActivity.Description, buySellActivity.Description);
			Assert.Equal(sendAndReceiveActivity.Fees, buySellActivity.Fees);
			Assert.Equal(sendAndReceiveActivity.Id, buySellActivity.Id);
			Assert.Equal(sendAndReceiveActivity.SortingPriority, buySellActivity.SortingPriority);
		}

		[Fact]
		public async Task Execute_WithSymbolProfile_ShouldReplaceGiftActivitiesWithBuySellActivities()
		{
			// Arrange
			var giftActivity = new Fixture().Create<GiftActivity>();

			var holdingWithSymbolProfile = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = [giftActivity]
			};

			// Act
			await _strategy.Execute(holdingWithSymbolProfile);

			// Assert
			Assert.Empty(holdingWithSymbolProfile.Activities.OfType<GiftActivity>());
			Assert.Single(holdingWithSymbolProfile.Activities.OfType<BuySellActivity>());

			var buySellActivity = holdingWithSymbolProfile.Activities.OfType<BuySellActivity>().First();
			Assert.Equal(giftActivity.Account, buySellActivity.Account);
			Assert.Equal(giftActivity.Date, buySellActivity.Date);
			Assert.Equal(giftActivity.Quantity, buySellActivity.Quantity);
			Assert.Equal(giftActivity.UnitPrice, buySellActivity.UnitPrice);
			Assert.Equal(giftActivity.TransactionId, buySellActivity.TransactionId);
			Assert.Equal(giftActivity.Description, buySellActivity.Description);
			Assert.Equal(giftActivity.Id, buySellActivity.Id);
			Assert.Equal(giftActivity.SortingPriority, buySellActivity.SortingPriority);
		}

		[Fact]
		public async Task Execute_WithoutSymbolProfile_ShouldReplaceGiftActivitiesWithInterestActivities()
		{
			// Arrange
			var giftActivity = new Fixture().Create<GiftActivity>();

			var holdingWithoutSymbolProfile = new Holding(null)
			{
				Activities = [giftActivity]
			};

			// Act
			await _strategy.Execute(holdingWithoutSymbolProfile);

			// Assert
			Assert.Empty(holdingWithoutSymbolProfile.Activities.OfType<GiftActivity>());
			Assert.Single(holdingWithoutSymbolProfile.Activities.OfType<InterestActivity>());

			var interestActivity = holdingWithoutSymbolProfile.Activities.OfType<InterestActivity>().First();
			Assert.Equal(giftActivity.Account, interestActivity.Account);
			Assert.Equal(giftActivity.Date, interestActivity.Date);
			Assert.Equal(giftActivity.Quantity, interestActivity.Amount.Amount);
			Assert.Equal(giftActivity.TransactionId, interestActivity.TransactionId);
			Assert.Equal(giftActivity.Description, interestActivity.Description);
			Assert.Equal(giftActivity.Id, interestActivity.Id);
			Assert.Equal(giftActivity.SortingPriority, interestActivity.SortingPriority);
		}

	}
}
