using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Activities
{
	internal class SymbolMatcherTask(ILogger<SymbolMatcherTask> logger, ISymbolMatcher[] symbolMatchers, IActivityRepository activityRepository/*, IMarketDataRepository marketDataRepository*/) : IScheduledWork
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

					var holding = await activityRepository.FindHolding(ids);

					if (holding != null)
					{
						holding.MergeIdentifiers(ids);
					}
					else
					{
						holding = new Holding
						{
							PartialSymbolIdentifiers = ids
						};
					}

					activity.Holding = holding;

					foreach (var symbolMatcher in symbolMatchers)
					{
						if (holding.SymbolProfiles.Any(x => x.DataSource == symbolMatcher.DataSource || (x.DataSource.StartsWith(ContractToModelMapper.DataSourcePrefix) && symbolMatcher.DataSource == Datasource.GHOSTFOLIO ) ))
						{
							continue;
						}

						var symbol = await symbolMatcher.MatchSymbol(ids.ToArray()).ConfigureAwait(false);

						if (symbol != null)
						{
							holding.SymbolProfiles.Add(symbol);
							logger.LogDebug($"Matched {symbol.Symbol} from {symbol.DataSource} with PartialIds {string.Join(",",ids.Select(x => x.Identifier))}");
						}
					}

					await activityRepository.Store(holding);
				}
			}
		}
	}
}
