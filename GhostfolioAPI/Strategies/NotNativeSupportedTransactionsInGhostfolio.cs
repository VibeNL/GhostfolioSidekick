using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Strategies;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class NotNativeSupportedTransactionsInGhostfolio() : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.NotNativeSupportedTransactionsInGhostfolio;

		public Task Execute(Holding holding)
		{
			ConvertSendAndRecievesToBuyAndSells(holding);
			ConvertGiftsToInterestOrBuy(holding);

			return Task.CompletedTask;
		}

		private static void ConvertSendAndRecievesToBuyAndSells(Holding holding)
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
		}

		private static void ConvertGiftsToInterestOrBuy(Holding holding)
		{
			var activities = holding.Activities.OfType<GiftActivity>().ToList();

			foreach (var activity in activities)
			{
				if (holding.SymbolProfile == null)
				{
					holding.Activities.Add(new InterestActivity(
						activity.Account,
						activity.Date,
						activity.UnitPrice!.Times(activity.Quantity),
						activity.TransactionId)
					{
						Description = activity.Description,
						Id = activity.Id,
						SortingPriority = activity.SortingPriority,
					});
				}
				else
				{
					holding.Activities.Add(new BuySellActivity(
					activity.Account,
					activity.Date,
					activity.Quantity,
					activity.UnitPrice,
					activity.TransactionId)
					{
						Description = activity.Description,
						Id = activity.Id,
						SortingPriority = activity.SortingPriority,
					});
				}

				holding.Activities.Remove(activity);
			}
		}
	}
}
