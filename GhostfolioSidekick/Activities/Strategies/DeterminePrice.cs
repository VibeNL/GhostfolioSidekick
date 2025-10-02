using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Activities.Strategies
{
	public class DeterminePrice : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.DeterminePrice;

		public Task Execute(Holding holding)
		{
			if (holding.SymbolProfiles.Count == 0)
			{
				return Task.CompletedTask;
			}

			var activities = holding.Activities
				.OrderBy(x => x.Date).ToList();

			if (activities.Count == 0)
			{
				return Task.CompletedTask;
			}

			foreach (var activity in activities.OfType<ActivityWithQuantityAndUnitPrice>())
			{
				switch (activity)
				{
					case ReceiveActivity:
					case SendActivity:
					case GiftAssetActivity:
					case StakingRewardActivity:
						SetUnitPrice(holding.SymbolProfiles, activity);
						break;
					default:
						break;
				}
			}

			return Task.CompletedTask;
		}

		private static void SetUnitPrice(List<SymbolProfile> symbolProfiles, ActivityWithQuantityAndUnitPrice activity)
		{
			foreach (var symbolProfile in symbolProfiles)
			{
				var marketData = symbolProfile
									.MarketData
									.OrderBy(x => x.Date)
									.FirstOrDefault(x => x.Date >= DateOnly.FromDateTime(activity.Date));
				if (marketData != null)
				{
					activity.AdjustedUnitPrice = marketData.Close;
					activity.AdjustedUnitPriceSource.Add(new CalculatedPriceTrace("Determine price", activity.AdjustedQuantity, activity.AdjustedUnitPrice));
					activity.TotalTransactionAmount = activity.AdjustedUnitPrice.Times(activity.AdjustedQuantity);
					return;
				}
			}
		}
	}
}
