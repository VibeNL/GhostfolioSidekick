using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class NotNativeSupportedTransactionsInGhostfolio() : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.NotNativeSupportedTransactionsInGhostfolio;

		public Task Execute(Holding holding)
		{
			ConvertSendAndRecievesToBuyAndSells(holding);
			ConvertGiftsToInterestOrBuy(holding);
			ConvertBondRepay(holding);

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
						new Money(activity.Account.Balance.Money.Currency, activity.Quantity),
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

		private static void ConvertBondRepay(Holding holding)
		{
			var activity = holding.Activities.OfType<RepayBondActivity>().SingleOrDefault();

			if (activity == null)
			{
				return;
			}

			var quantity = holding.Activities.OfType<BuySellActivity>().Sum(x => x.Quantity);
			var price = activity.TotalRepayAmount.SafeDivide(quantity);

			holding.Activities.Add(new BuySellActivity(
				activity.Account,
				activity.Date,
				-quantity,
				price,
				activity.TransactionId)
			{
				Description = activity.Description,
				Id = activity.Id,
				SortingPriority = activity.SortingPriority,
			});
			holding.Activities.Remove(activity);
		}
	}
}
