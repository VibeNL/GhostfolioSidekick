using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Activities
{
	public class SymbolMatcherTask(
			ILogger<SymbolMatcherTask> logger,
			IApplicationSettings applicationSettings,
			ISymbolMatcher[] symbolMatchers,
			IDbContextFactory<DatabaseContext> databaseContextFactory) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SymbolMatcher;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;
		public async Task DoWork()
		{
			using var databaseContext = databaseContextFactory.CreateDbContext();
			var activities = await databaseContext.Activities.ToListAsync();

			var currentHoldings = await databaseContext.Holdings.ToListAsync();

			foreach (var activityTuple in activities
					.Select(x => new CustomObject { Activity = x, PartialIdentifier = x as IActivityWithPartialIdentifier })
					.Where(x => x.PartialIdentifier is not null)
					.OrderBy(x => GetIds(x.PartialIdentifier!.PartialSymbolIdentifiers).FirstOrDefault()?.Identifier))
			{
				await HandleActivity(currentHoldings, activityTuple).ConfigureAwait(false);
			}

			AssertNoMultipleSymbols(logger, currentHoldings);

			await databaseContext.SaveChangesAsync();
		}

		private static void AssertNoMultipleSymbols(ILogger<SymbolMatcherTask> logger, List<Holding> currentHoldings)
		{
			var allsymbols = currentHoldings.SelectMany(x => x.SymbolProfiles).GroupBy(x => new { x.Symbol, x.DataSource }).Where(x => x.Count() > 1).ToList();

			foreach (var item in allsymbols)
			{
				// find holding
				var holdings = currentHoldings.Where(x => x.SymbolProfiles.Any(y => y.DataSource == item.Key.DataSource && y.Symbol == item.Key.Symbol)).ToList();
				foreach (var holding in holdings)
				{
					logger.LogError($"Multiple symbols found for {item.Key.Symbol} from {item.Key.DataSource}");
				}
			}

			if (allsymbols.Count != 0)
			{
				throw new NotSupportedException("Multiple symbols found");
			}
		}

		private async Task HandleActivity(List<Holding> currentHoldings, CustomObject activityTuple)
		{
			var activity = activityTuple.Activity;
			var ids = GetIds(activityTuple.PartialIdentifier!.PartialSymbolIdentifiers);

			if (ids == null || ids.Count == 0)
			{
				return;
			}

			// Match on existing holdings with ids
			var matchingHoldings = currentHoldings.Where(x => ids.Any(y => x.IdentifierContainsInList(y))).ToList();

			if (matchingHoldings.Count > 1)
			{
				logger.LogWarning("Multiple holdings found for {Identifiers}", string.Join(",", ids.Select(x => x.Identifier)));
				return;
			}

			var holding = matchingHoldings.SingleOrDefault();

			if (holding != null)
			{
				activity.Holding = holding;
				holding.MergeIdentifiers(ids);
				return;
			}

			// Find symbol via symbolMatchers
			var symbols = new List<SymbolProfile>();
			foreach (var symbolMatcher in symbolMatchers)
			{
				// Match symbol
				var symbol = await symbolMatcher.MatchSymbol([.. ids]).ConfigureAwait(false);

				if (symbol != null)
				{
					symbols.Add(symbol);
				}
			}

			if (symbols.Count == 0)
			{
				logger.LogWarning("No symbol found for {Identifiers}", string.Join(",", ids.Select(x => x.Identifier)));
				return;

			}

			foreach (var symbol in symbols)
			{
				// Try to find existing holding
				holding ??= currentHoldings.SingleOrDefault(x => CompareSymbolName(x, symbol));
				holding ??= currentHoldings.SingleOrDefault(x => symbol.Identifiers.Select(x => PartialSymbolIdentifier.CreateGeneric(x)).Any(y => x.IdentifierContainsInList(y)));
			}

			// Create new holding if not found, should not happen
			if (holding == null)
			{
				logger.LogWarning("No holding found for {Identifiers}", string.Join(",", ids.Select(x => x.Identifier)));
				return;
			}

			foreach (var symbol in symbols)
			{
				// Merge identifiers
				holding.MergeIdentifiers(GetIdentifiers(symbol));

				// Add symbol to holding
				if (!holding.SymbolProfiles.Any(y => y.DataSource == symbol.DataSource && CompareSymbolName(y.Symbol, symbol.Symbol)))
				{
					holding.SymbolProfiles.Add(symbol);
				}

				logger.LogDebug($"Matched {symbol.Symbol} from {symbol.DataSource} with PartialIds {string.Join(",", ids.Select(x => x.Identifier))}");
			}

			activity.Holding = holding;
		}

		private static bool CompareSymbolName(Holding x, SymbolProfile symbol)
		{
			return x.SymbolProfiles.Any(y => y.DataSource == symbol.DataSource && CompareSymbolName(y.Symbol, symbol.Symbol));
		}

		private static bool CompareSymbolName(string a, string b)
		{
			return string.Equals(
					a.Replace("-", string.Empty),
					b.Replace("-", string.Empty),
					StringComparison.InvariantCultureIgnoreCase);
		}

		private static IList<PartialSymbolIdentifier> GetIdentifiers(SymbolProfile symbol)
		{
			var lst = new List<PartialSymbolIdentifier>();
			foreach (var item in symbol.Identifiers)
			{
				lst.Add(new PartialSymbolIdentifier
				{
					Identifier = item,
					AllowedAssetClasses = [symbol.AssetClass],
					AllowedAssetSubClasses = symbol.AssetSubClass != null ? [symbol.AssetSubClass.Value] : null
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

			return [.. ids.DistinctBy(x => x.Identifier)];
		}

		private class CustomObject
		{
			public Activity Activity { get; set; } = default!;
			public IActivityWithPartialIdentifier? PartialIdentifier { get; set; } = default!;
		}
	}
}
