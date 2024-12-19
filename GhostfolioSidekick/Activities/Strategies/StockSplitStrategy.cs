using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Activities.Strategies
{
	public class StockSplitStrategy : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.StockSplit;

		public Task Execute(Holding holding)
		{
			var stockSplits = holding.SymbolProfiles.SelectMany(x => x.StockSplits).ToList();
			if (stockSplits.Count == 0)
			{
				return Task.CompletedTask;
			}

			foreach (var split in stockSplits)
			{
				var splitFactor = split.BeforeSplit / (decimal)split.AfterSplit;
				var inverseSplitFactor = split.AfterSplit / (decimal)split.BeforeSplit;

				foreach (var activity in holding.Activities.Where(x => x.Date < split.Date.ToDateTime(TimeOnly.MinValue)).OfType<ActivityWithQuantityAndUnitPrice>())
				{
					activity.AdjustedUnitPrice = activity!.AdjustedUnitPrice?.Times(splitFactor);
					activity.AdjustedQuantity = activity!.AdjustedQuantity * inverseSplitFactor;
					activity.AdjustedUnitPriceSource.Add(new CalculatedPriceTrace(split.ToString(), activity.AdjustedQuantity, activity.AdjustedUnitPrice));
				}
			}

			return Task.CompletedTask;
		}
	}
}
