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
				if (activity is IActivityWithPartialIdentifier activityWithPartialIdentifier)
				{
					var ids = activityWithPartialIdentifier.PartialSymbolIdentifiers;
					var symbol = await symbolMatcher.MatchSymbol(ids.ToArray()).ConfigureAwait(false);

					if (symbol != null)
					{
						await marketDataRepository.Store(symbol);
						await activityRepository.SetMatch(activity, symbol);
					}
				}
			}
		}
	}
}
