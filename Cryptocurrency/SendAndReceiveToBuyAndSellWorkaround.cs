using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Strategies;

namespace GhostfolioSidekick.Cryptocurrency
{
	public class SendAndReceiveToBuyAndSellWorkaround() : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.SendAndReceiveToBuyAndSell;

		public Task Execute(Holding holding)
		{
			var activities = holding.Activities.OfType<SendAndReceiveActivity>().ToList();

			foreach (var activity in activities)
			{
				holding.Activities.Add(new BuySellActivity(
					activity.Account,
					activity.Date,
					activity.Quantity,
					activity.UnitPrice,
					activity.TransactionId)
				{
					Description = activity.Description,
					Fees = activity.Fees,
					Id = activity.Id,
					SortingPriority = activity.SortingPriority,
				});
				holding.Activities.Remove(activity);
			}

			return Task.CompletedTask;
		}
	}
}
