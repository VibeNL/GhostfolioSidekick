using GhostfolioSidekick.Activities.Comparer;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Activities
{
	public class DetermineHoldings(
	ISymbolMatcher[] symbolMatchers,
	IDbContextFactory<DatabaseContext> databaseContextFactory,
	IMemoryCache memoryCache,
	IApplicationSettings settings) : IScheduledWork
	{
		private readonly Mapping[] mappings = settings?.ConfigurationInstance?.Mappings ?? [];
		private readonly string primaryCurrency = settings?.ConfigurationInstance?.Settings?.PrimaryCurrency ?? string.Empty;

		public TaskPriority Priority => TaskPriority.DetermineHoldings;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Determine Holdings";

		public async Task DoWork(ILogger logger)
		{
			await ClearExistingHoldings();

			List<int> usedHoldingIds;
			using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				var activities = await databaseContext.Activities.ToListAsync();

				// Load existing holdings to potentially reuse their IDs
				var existingHoldings = await databaseContext.Holdings.ToListAsync();
				var availableHoldings = new Queue<Holding>(existingHoldings);
				var usedHoldings = new List<Holding>();
				var partialIdentifierMap = new Dictionary<PartialSymbolIdentifier, Holding>(new PartialSymbolIdentifierComparer());
				var symbolProfileMap = new Dictionary<string, Holding>(); // Track holdings by symbol+datasource

				var validPartialIdentifiers = new List<IList<PartialSymbolIdentifier>>();
				foreach (var activity in activities.OfType<IActivityWithPartialIdentifier>())
				{
					var partialIdentifiers = activity.PartialSymbolIdentifiers;
					if (partialIdentifiers == null || partialIdentifiers.Count == 0)
					{
						logger.LogWarning("DetermineHoldings: Activity {ActivityType} has null or empty PartialSymbolIdentifiers. Activity details: {@Activity}", activity.GetType().Name, activity);
						continue;
					}

					validPartialIdentifiers.Add(partialIdentifiers);
				}

				IEnumerable<IList<PartialSymbolIdentifier>> partialIdentifiersList = validPartialIdentifiers
					.GroupBy(x => string.Join("|", x.Select(id => id.Identifier).OrderBy(id => id)))
					.Select(g => (IList<PartialSymbolIdentifier>)[.. g.SelectMany(ids => ids)
					.OrderBy(id => GetIdentifierTypePriority(id.IdentifierType))
					.ThenBy(id => GetCurrencyPriority(id.Currency))])
					.OrderBy(x => GetIdentifierTypePriority(x[0].IdentifierType))
					.ThenBy(x => GetCurrencyPriority(x[0].Currency))
					.ThenBy(x => x[0].Identifier)
					.ToList();
				foreach (var partialIdentifiers in partialIdentifiersList)
				{
					var ids = GetIds(partialIdentifiers);
					await CreateOrReuseHolding(logger, databaseContext, partialIdentifierMap, symbolProfileMap, availableHoldings, usedHoldings, ids).ConfigureAwait(false);
				}

				// Remove any unused holdings
				var unusedHoldings = availableHoldings.ToList();
				if (unusedHoldings.Count > 0)
				{
					logger.LogInformation("Removing {Count} unused holdings", unusedHoldings.Count);
					databaseContext.Holdings.RemoveRange(unusedHoldings);
				}

				await databaseContext.SaveChangesAsync();
				usedHoldingIds = [.. usedHoldings.Select(h => h.Id)];
			}

			using var matchingContext = await databaseContextFactory.CreateDbContextAsync();
			var holdingsForMatching = await matchingContext.Holdings
				.Where(h => usedHoldingIds.Contains(h.Id))
				.ToListAsync();
			await MatchOtherMatchers(logger, matchingContext, holdingsForMatching).ConfigureAwait(false);
		}

		private async Task MatchOtherMatchers(ILogger logger, DatabaseContext databaseContext, List<Holding> usedHoldings)
		{
			foreach (var holding in usedHoldings)
			{
				foreach (var symbolMatcher in symbolMatchers.Where(x => !x.AllowedForDeterminingHolding))
				{
					var cacheKey = $"{nameof(DetermineHoldings)}|{symbolMatcher.GetType()}|{string.Join(",", holding.PartialSymbolIdentifiers)}";
					if (!memoryCache.TryGetValue<SymbolProfile>(cacheKey, out var symbolProfile))
					{
						var orderedIdentifiers = holding.PartialSymbolIdentifiers
						.OrderBy(x => GetIdentifierTypePriority(x.IdentifierType))
						.ThenBy(x => GetCurrencyPriority(x.Currency))
						.ToArray();
						symbolProfile = await symbolMatcher.MatchSymbol(orderedIdentifiers).ConfigureAwait(false);
						memoryCache.Set(cacheKey, symbolProfile, CacheDuration.Short());
					}

					if (symbolProfile != null)
					{
						// FindAsync checks the identity map (including Added entities) before hitting the DB,
						// ensuring we always use the single tracked instance for a given {Symbol, DataSource} key.
						var existingSymbolProfile = await databaseContext.SymbolProfiles.FindAsync(symbolProfile.Symbol, symbolProfile.DataSource).ConfigureAwait(false);
						symbolProfile = existingSymbolProfile ?? symbolProfile;
						logger.LogDebug("Matching additional symbol profile for holding {HoldingId}: {Symbol} ({DataSource})", holding.Id, symbolProfile.Symbol, symbolProfile.DataSource);
						holding.MergeSymbolProfiles(symbolProfile);
					}
				}

				await databaseContext.SaveChangesAsync();
			}
		}

		private async Task ClearExistingHoldings()
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var existingHoldings = await databaseContext.Holdings.ToListAsync();

			// Reset all existing holdings to a clean state
			foreach (var holding in existingHoldings)
			{
				holding.SymbolProfiles.Clear();
				holding.PartialSymbolIdentifiers.Clear();
				holding.Activities.Clear();
			}

			await databaseContext.SaveChangesAsync();
		}

		private async Task CreateOrReuseHolding(
		ILogger logger,
		DatabaseContext databaseContext,
		Dictionary<PartialSymbolIdentifier, Holding> partialIdentifierMap,
		Dictionary<string, Holding> symbolProfileMap,
		Queue<Holding> availableHoldings,
		List<Holding> usedHoldings,
		IList<PartialSymbolIdentifier> partialIdentifiers)
		{
			var found = false;
			foreach (var symbolMatcher in symbolMatchers.Where(x => x.AllowedForDeterminingHolding))
			{
				var cacheKey = $"{nameof(DetermineHoldings)}|{symbolMatcher.GetType()}|{string.Join(",", partialIdentifiers)}";
				if (!memoryCache.TryGetValue<SymbolProfile>(cacheKey, out var symbolProfile))
				{
					symbolProfile = await symbolMatcher.MatchSymbol([.. partialIdentifiers]).ConfigureAwait(false);
					memoryCache.Set(cacheKey, symbolProfile, CacheDuration.Short());
				}

				if (symbolProfile != null)
				{
					// FindAsync checks the identity map (including Added entities) before hitting the DB,
					// ensuring we always use the single tracked instance for a given {Symbol, DataSource} key.
					var existingSymbolProfile = await databaseContext.SymbolProfiles.FindAsync(symbolProfile.Symbol, symbolProfile.DataSource).ConfigureAwait(false);
					symbolProfile = existingSymbolProfile ?? symbolProfile;
				}

				if (symbolProfile == null)
				{
					continue;
				}

				found = true;

				var symbolKey = $"{symbolProfile.Symbol}|{symbolProfile.DataSource}";

				// First check if we already have a holding for this specific partial identifier
				if (FindHolding(partialIdentifierMap, partialIdentifiers, out var existingHolding) && existingHolding != null)
				{
					logger.LogTrace("CreateOrReuseHolding: Merging identifiers for existing holding with symbol {Symbol}", symbolProfile.Symbol);
					existingHolding.MergeSymbolProfiles(symbolProfile);
					existingHolding.MergeIdentifiers(partialIdentifiers);

					AddPartialIdentifiersToMap(partialIdentifierMap, partialIdentifiers, existingHolding);
					// Also update the symbol map
					if (!symbolProfileMap.ContainsKey(symbolKey))
					{
						symbolProfileMap[symbolKey] = existingHolding;
					}

					continue;
				}

				// Then check if we already have a holding for this symbol profile
				if (symbolProfileMap.TryGetValue(symbolKey, out existingHolding))
				{
					logger.LogTrace("CreateOrReuseHolding: Merging identifiers for existing holding with symbol {Symbol}", symbolProfile.Symbol);
					existingHolding.MergeSymbolProfiles(symbolProfile);
					existingHolding.MergeIdentifiers(partialIdentifiers);

					AddPartialIdentifiersToMap(partialIdentifierMap, partialIdentifiers, existingHolding);

					continue;
				}

				logger.LogDebug("CreateOrReuseHolding: Creating new holding for symbol {Symbol}", symbolProfile.Symbol);

				// Try to reuse an existing holding, otherwise create a new one
				Holding holding;
				if (availableHoldings.TryDequeue(out var reusedHolding))
				{
					logger.LogTrace("CreateOrReuseHolding: Reusing existing holding (ID: {HoldingId}) for symbol {Symbol}", reusedHolding.Id, symbolProfile.Symbol);
					holding = reusedHolding;
				}
				else
				{
					logger.LogTrace("CreateOrReuseHolding: Creating new holding for symbol {Symbol}", symbolProfile.Symbol);
					holding = new Holding();
					databaseContext.Holdings.Add(holding);
				}

				holding.MergeSymbolProfiles(symbolProfile);
				holding.MergeIdentifiers(partialIdentifiers);

				AddPartialIdentifiersToMap(partialIdentifierMap, partialIdentifiers, holding);
				symbolProfileMap[symbolKey] = holding;
				usedHoldings.Add(holding);
			}

			if (!found)
			{
				logger.LogWarning("CreateOrReuseHolding: No symbol profile found for {PartialIdentifiers}", string.Join(", ", partialIdentifiers));
			}
		}

		private static void AddPartialIdentifiersToMap(
		Dictionary<PartialSymbolIdentifier, Holding> partialIdentifierMap,
		IList<PartialSymbolIdentifier> partialIdentifiers,
		Holding existingHolding)
		{
			foreach (var identifier in partialIdentifiers)
			{
				if (!partialIdentifierMap.ContainsKey(identifier))
				{
					partialIdentifierMap[identifier] = existingHolding;
				}
			}
		}

		private static bool FindHolding(
		Dictionary<PartialSymbolIdentifier, Holding> partialIdentifierMap,
		IList<PartialSymbolIdentifier> partialIdentifiers,
		out Holding? existingHolding)
		{
			existingHolding = null;

			// Try to find an existing holding by any of the provided partial identifiers
			foreach (var identifier in partialIdentifiers)
			{
				if (partialIdentifierMap.TryGetValue(identifier, out existingHolding))
				{
					return true;
				}
			}

			return false;
		}

		private IList<PartialSymbolIdentifier> GetIds(IList<PartialSymbolIdentifier> partialSymbolIdentifiers)
		{
			var ids = new List<PartialSymbolIdentifier>();
			foreach (var partialIdentifier in partialSymbolIdentifiers)
			{
				ids.Add(partialIdentifier);

				if (mappings.FirstOrDefault(x => x.Source == partialIdentifier.Identifier) is Mapping mapping)
				{
					ids.Add(partialIdentifier with { Identifier = mapping.Target });
				}
			}

			return [.. ids
.OrderBy(x => GetIdentifierTypePriority(x.IdentifierType))
.ThenBy(x => GetCurrencyPriority(x.Currency))
.DistinctBy(x => x.Identifier)];
		}

		private static int GetIdentifierTypePriority(IdentifierType identifierType) => identifierType switch
		{
			IdentifierType.Ticker => 0,
			IdentifierType.ISIN => 1,
			IdentifierType.Default => 2,
			IdentifierType.Name => 3,
			_ => 4,
		};

		private int GetCurrencyPriority(Currency? currency) => currency switch
		{
			null => 2,
			_ when currency.Symbol == primaryCurrency => 0,
			_ => 1,
		};
	}
}