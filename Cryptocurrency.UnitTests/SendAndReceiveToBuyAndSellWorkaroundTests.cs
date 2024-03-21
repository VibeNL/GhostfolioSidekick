using AutoFixture;
using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.UnitTests.Cryptocurrency
{
	public class SendAndReceiveToBuyAndSellWorkaroundTests
	{
		private readonly SendAndReceiveToBuyAndSellWorkaround _strategy;

		public SendAndReceiveToBuyAndSellWorkaroundTests()
		{
			_strategy = new SendAndReceiveToBuyAndSellWorkaround();
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
	}
}
