using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class SymbolMatcherTask(ISymbolMatcher symbolMatcher, IActivityRepository activityRepository, IMarketDataRepository marketDataRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SymbolMapper;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			var activities = await activityRepository.GetAllActivities();

			var currencies = new Dictionary<Currency, DateTime>();
			foreach (var activity in activities.OrderBy(x => x.Date))
			{
				switch (activity)
				{
					case ActivityWithQuantityAndUnitPrice activityWithQuantityAndUnitPrice:
						{
							var ids = activityWithQuantityAndUnitPrice.PartialSymbolIdentifiers;
							var symbol = await symbolMatcher.MatchSymbol(ids.ToArray()).ConfigureAwait(false);

							if (symbol != null)
							{
								symbol.MergeKnownIdentifiers(ids);
								await marketDataRepository.Store(symbol);
							}
						}
						break;
					default:
						break;
				}
			}
		}
	}
}
