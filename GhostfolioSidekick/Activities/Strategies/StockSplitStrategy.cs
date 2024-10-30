using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GhostfolioSidekick.Activities.Strategies
{
	public class StockSplitStrategy(IActivityRepository activityRepository) : IHoldingStrategy
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
					activity.CalculatedUnitPrice = activity!.CalculatedUnitPrice?.Times(splitFactor);
					activity.CalculatedQuantity = activity!.CalculatedQuantity * inverseSplitFactor;
					activity.CalculatedUnitPriceSource.Add(new CalculatedPriceTrace(split.ToString(), activity.CalculatedQuantity, activity.CalculatedUnitPrice));
				}
			}

			activityRepository.Store(holding);
			return Task.CompletedTask;
		}
	}
}
