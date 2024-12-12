using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Caching.Memory;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Activities
{
	public class SymbolMatcherTask(
			IMemoryCache memoryCache,
			ILogger<SymbolMatcherTask> logger,
			IApplicationSettings applicationSettings,
			ISymbolMatcher[] symbolMatchers,
			IDbContextFactory<DatabaseContext> databaseContextFactory) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SymbolMatcher;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		public async Task DoWork()
		{
			using var databaseContext = databaseContextFactory.CreateDbContext();
			var activities = await databaseContext.Activities.ToListAsync();

			var existingHoldings = await databaseContext.Holdings.ToListAsync();

			var currencies = new Dictionary<Currency, DateTime>();
			foreach (var activity in activities.OrderBy(x => x.Date))
			{
				if (activity is IActivityWithPartialIdentifier activityWithPartialIdentifier)
				{
					var ids = GetIds(activityWithPartialIdentifier.PartialSymbolIdentifiers);

					if (ids == null || ids.Count == 0)
					{
						continue;
					}

					// Match on existing holdings with ids
					var holding = existingHoldings.SingleOrDefault(x => ids.Any(y => x.IdentifierContainsInList(y)));
					if (holding != null)
					{
						activity.Holding = holding;
						holding.MergeIdentifiers(ids);
						continue;
					}

					// Find symbol via symbolMatchers
					foreach (var symbolMatcher in symbolMatchers)
					{
						if (holding?.SymbolProfiles.Any(x => x.DataSource == symbolMatcher.DataSource || (Datasource.IsGhostfolio(x.DataSource) && symbolMatcher.DataSource == Datasource.GHOSTFOLIO)) ?? false)
						{
							continue;
						}

						var cacheKey = $"{symbolMatcher.DataSource}_{string.Join(",", ids)}";

						if (memoryCache.TryGetValue(cacheKey, out SymbolProfile? match) && match == null)
						{
							continue;
						}

						var symbol = await symbolMatcher.MatchSymbol([.. ids]).ConfigureAwait(false);

						if (symbol != null)
						{
							holding ??= existingHoldings.SingleOrDefault(x => CompareSymbolName1(x, symbol));
							holding ??= existingHoldings.SingleOrDefault(x => symbol.Identifiers.Select(x => PartialSymbolIdentifier.CreateGeneric(x)).Any(y => x.IdentifierContainsInList(y)));

							if (holding == null)
							{
								holding = new Holding();
								holding.MergeIdentifiers(ids);
								databaseContext.Holdings.Add(holding);
								existingHoldings.Add(holding);
							}

							holding.MergeIdentifiers(GetIdentifiers(symbol));

							if (!holding.SymbolProfiles.Any(y => y.DataSource == symbol.DataSource && CompareSymbolName(y.Symbol, symbol.Symbol)))
							{
								holding.SymbolProfiles.Add(symbol);
							}

							logger.LogDebug($"Matched {symbol.Symbol} from {symbol.DataSource} with PartialIds {string.Join(",", ids.Select(x => x.Identifier))}");
						}

						memoryCache.Set(cacheKey, symbol, TimeSpan.FromHours(1));
					}

					activity.Holding = holding;
				}
			}

			var allsymbols = existingHoldings.SelectMany(x => x.SymbolProfiles).GroupBy(x => new { x.Symbol, x.DataSource }).Where(x => x.Count() > 1).ToList();

			foreach (var item in allsymbols)
			{
				// find holding
				var holding = existingHoldings.Where(x => x.SymbolProfiles.Any(y => y.DataSource == item.Key.DataSource && y.Symbol == item.Key.Symbol)).ToList();
			}

			await databaseContext.SaveChangesAsync();
		}

		private bool CompareSymbolName1(Holding x, SymbolProfile symbol)
		{
			return x.SymbolProfiles.Any(y => y.DataSource == symbol.DataSource && CompareSymbolName(y.Symbol, symbol.Symbol));
		}

		private bool CompareSymbolName(string a, string b)
		{
			return string.Equals(
					a.Replace("-", string.Empty),
					b.Replace("-", string.Empty),
					StringComparison.InvariantCultureIgnoreCase);
		}

		private IList<PartialSymbolIdentifier> GetIdentifiers(SymbolProfile symbol)
		{
			var lst = new List<PartialSymbolIdentifier>();
			foreach (var item in symbol.Identifiers)
			{
				lst.Add(new PartialSymbolIdentifier
				{
					Identifier = item,
					AllowedAssetClasses = [symbol.AssetClass],
					AllowedAssetSubClasses = symbol.AssetSubClass != null ?[symbol.AssetSubClass.Value] : null
				});
			}

			return lst;
		}

		private IList<PartialSymbolIdentifier> GetIds(IList<PartialSymbolIdentifier> partialSymbolIdentifiers)
		{
			var mappings = applicationSettings?.ConfigurationInstance?.Mappings ?? [];

			var ids = new List<PartialSymbolIdentifier>();
			foreach (var partialIdentifier in partialSymbolIdentifiers)
			{
				ids.Add(partialIdentifier);

				if (mappings.FirstOrDefault(x => x.Source == partialIdentifier.Identifier) is Mapping mapping)
				{
					ids.Add(partialIdentifier with { Identifier = mapping.Target });
				}
			}

			return ids.DistinctBy(x => x.Identifier).ToList();
		}
	}
}
