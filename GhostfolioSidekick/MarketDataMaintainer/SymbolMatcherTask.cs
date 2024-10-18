using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class SymbolMatcherTask(ISymbolMatcher[] symbolMatchers, IActivityRepository activityRepository, IMarketDataRepository marketDataRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SymbolMatcher;

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

					if (ids == null || ids.Count == 0)
					{
						continue;
					}

					foreach(var symbolMatcher in symbolMatchers)
					{
						if (await activityRepository.HasMatch(ids, symbolMatcher.DataSource))
						{
							continue;
						}

						var symbol = await symbolMatcher.MatchSymbol([.. ids]).ConfigureAwait(false);

						if (symbol != null)
						{
							await marketDataRepository.Store(symbol);
							await activityRepository.SetMatch(activity, symbol);
							break;
						}
					}
				}
			}
		}
	}
}
