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
using System.Security.Cryptography;
using System.Text;

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
			// Preload all SymbolProfiles into a dictionary for O(1) lookup
			Dictionary<string, SymbolProfile> allSymbolProfiles;
			using (DatabaseContext preloadContext = await databaseContextFactory.CreateDbContextAsync())
			{
				allSymbolProfiles = preloadContext.SymbolProfiles != null
					? await preloadContext.SymbolProfiles
						.AsNoTracking()
						.ToDictionaryAsync(
							sp => $"{sp.Symbol}|{sp.DataSource}",
							sp => sp
						)
					: [];
			}

			using (DatabaseContext databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				List<Activity> activities = await databaseContext.Activities.ToListAsync();

				// Load existing holdings to potentially reuse their IDs
				List<Holding> existingHoldings = await databaseContext.Holdings.ToListAsync();
				var availableHoldings = new Queue<Holding>(existingHoldings);
				var usedHoldings = new List<Holding>();
				var partialIdentifierMap = new Dictionary<PartialSymbolIdentifier, Holding>(new PartialSymbolIdentifierComparer());
				var symbolProfileMap = new Dictionary<string, Holding>(); // Track holdings by symbol+datasource

				var validPartialIdentifiers = new List<IList<PartialSymbolIdentifier>>();
				foreach (IActivityWithPartialIdentifier activity in activities.OfType<IActivityWithPartialIdentifier>())
				{
					List<PartialSymbolIdentifier> partialIdentifiers = activity.PartialSymbolIdentifiers;
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
				// Tracks the single authoritative SymbolProfile instance per {Symbol|DataSource} key within this context,
				// preventing EF from tracking two different C# objects for the same composite key.
				var resolvedProfilesMap = new Dictionary<string, SymbolProfile>();
				foreach (IList<PartialSymbolIdentifier> partialIdentifiers in partialIdentifiersList)
				{
					IList<PartialSymbolIdentifier> ids = GetIds(partialIdentifiers);
					await CreateOrReuseHolding(logger, databaseContext, resolvedProfilesMap, partialIdentifierMap, symbolProfileMap, availableHoldings, usedHoldings, ids, allSymbolProfiles).ConfigureAwait(false);
				}

				// Remove any unused holdings
				var unusedHoldings = availableHoldings.ToList();
				if (unusedHoldings.Count > 0)
				{
					logger.LogInformation("Removing {Count} unused holdings", unusedHoldings.Count);
					databaseContext.Holdings.RemoveRange(unusedHoldings);
				}

				// Only one SaveChangesAsync here for batching
				_ = await databaseContext.SaveChangesAsync();
				usedHoldingIds = [.. usedHoldings.Select(h => h.Id)];
			}

			using DatabaseContext matchingContext = await databaseContextFactory.CreateDbContextAsync();
			List<Holding> holdingsForMatching = await matchingContext.Holdings
			.Include(h => h.SymbolProfiles)
			   .Where(h => usedHoldingIds.Contains(h.Id))
			   .ToListAsync();
			await MatchOtherMatchers(logger, matchingContext, holdingsForMatching, allSymbolProfiles).ConfigureAwait(false);
		}

		private async Task MatchOtherMatchers(ILogger logger, DatabaseContext databaseContext, List<Holding> usedHoldings, Dictionary<string, SymbolProfile> allSymbolProfiles)
		{
			var resolvedProfilesMap = new Dictionary<string, SymbolProfile>();
			foreach (Holding holding in usedHoldings)
			{
				foreach (ISymbolMatcher? symbolMatcher in symbolMatchers.Where(x => !x.AllowedForDeterminingHolding))
				{
					var cacheKey = GetCacheKey(nameof(DetermineHoldings), symbolMatcher.GetType(), holding.PartialSymbolIdentifiers);
					if (!memoryCache.TryGetValue<(string Symbol, string DataSource)>(cacheKey, out (string Symbol, string DataSource) symbolProfileKey))
					{
						PartialSymbolIdentifier[] orderedIdentifiers = holding.PartialSymbolIdentifiers
						   .OrderBy(x => GetIdentifierTypePriority(x.IdentifierType))
						   .ThenBy(x => GetCurrencyPriority(x.Currency))
						   .ToArray();
						SymbolProfile? symbolProfile = await symbolMatcher.MatchSymbol(orderedIdentifiers).ConfigureAwait(false);
						if (symbolProfile != null)
						{
							symbolProfileKey = (symbolProfile.Symbol, symbolProfile.DataSource);
							_ = memoryCache.Set(cacheKey, symbolProfileKey, CacheDuration.Short());
						}
						else
						{
							symbolProfileKey = default;
						}
					}

					if (symbolProfileKey != default)
					{
						(string? symbol, string? dataSource) = symbolProfileKey;
						if (!resolvedProfilesMap.TryGetValue($"{symbol}|{dataSource}", out SymbolProfile? resolvedProfile))
						{
							if (!allSymbolProfiles.TryGetValue($"{symbol}|{dataSource}", out resolvedProfile))
							{
								// If not found, try to get a fresh SymbolProfile from the matcher
								PartialSymbolIdentifier[] orderedIdentifiers = holding.PartialSymbolIdentifiers
								   .OrderBy(x => GetIdentifierTypePriority(x.IdentifierType))
								   .ThenBy(x => GetCurrencyPriority(x.Currency))
								   .ToArray();
								resolvedProfile = await symbolMatcher.MatchSymbol(orderedIdentifiers).ConfigureAwait(false);
								if (resolvedProfile != null)
								{
									_ = databaseContext.SymbolProfiles.Add(resolvedProfile);
									allSymbolProfiles[$"{symbol}|{dataSource}"] = resolvedProfile;
								}
							}
							if (resolvedProfile != null)
							{
								resolvedProfilesMap[$"{symbol}|{dataSource}"] = resolvedProfile;
							}
						}
						if (resolvedProfile != null)
						{
							logger.LogDebug("Matching additional symbol profile for holding {HoldingId}: {Symbol} ({DataSource})", holding.Id, resolvedProfile.Symbol, resolvedProfile.DataSource);
							holding.MergeSymbolProfiles(resolvedProfile);
						}
					}
				}
			}

			_ = await databaseContext.SaveChangesAsync();
		}

		private async Task ClearExistingHoldings()
		{
			using DatabaseContext databaseContext = await databaseContextFactory.CreateDbContextAsync();
			List<Holding> existingHoldings = await databaseContext.Holdings
				.Include(h => h.SymbolProfiles)
				.Include(h => h.Activities)
				.AsSplitQuery()
				.ToListAsync();

			// Reset all existing holdings to a clean state
			foreach (Holding? holding in existingHoldings)
			{
				holding.SymbolProfiles.Clear();
				holding.PartialSymbolIdentifiers.Clear();
				holding.Activities.Clear();
			}

			_ = await databaseContext.SaveChangesAsync();
		}

		private async Task CreateOrReuseHolding(
		ILogger logger,
		DatabaseContext databaseContext,
		Dictionary<string, SymbolProfile> resolvedProfilesMap,
		Dictionary<PartialSymbolIdentifier, Holding> partialIdentifierMap,
		Dictionary<string, Holding> symbolProfileMap,
		Queue<Holding> availableHoldings,
		List<Holding> usedHoldings,
		IList<PartialSymbolIdentifier> partialIdentifiers,
		Dictionary<string, SymbolProfile> allSymbolProfiles)
		{
			var found = false;
			foreach (ISymbolMatcher? symbolMatcher in symbolMatchers.Where(x => x.AllowedForDeterminingHolding))
			{
				var cacheKey = GetCacheKey(nameof(DetermineHoldings), symbolMatcher.GetType(), partialIdentifiers);
				if (!memoryCache.TryGetValue<(string Symbol, string DataSource)>(cacheKey, out (string Symbol, string DataSource) symbolProfileKey))
				{
					SymbolProfile? symbolProfile = await symbolMatcher.MatchSymbol([.. partialIdentifiers]).ConfigureAwait(false);
					if (symbolProfile != null)
					{
						symbolProfileKey = (symbolProfile.Symbol, symbolProfile.DataSource);
						_ = memoryCache.Set(cacheKey, symbolProfileKey, CacheDuration.Short());
					}
					else
					{
						symbolProfileKey = default;
					}
				}

				if (symbolProfileKey == default)
				{
					continue;
				}

				(string? symbol, string? dataSource) = symbolProfileKey;
				if (!resolvedProfilesMap.TryGetValue($"{symbol}|{dataSource}", out SymbolProfile? resolvedProfile))
				{
					if (!allSymbolProfiles.TryGetValue($"{symbol}|{dataSource}", out resolvedProfile))
					{
						SymbolProfile? symbolProfile = await symbolMatcher.MatchSymbol([.. partialIdentifiers]).ConfigureAwait(false);
						if (symbolProfile != null)
						{
							_ = databaseContext.SymbolProfiles.Add(symbolProfile);
							resolvedProfile = symbolProfile;
							allSymbolProfiles[$"{symbol}|{dataSource}"] = resolvedProfile;
						}
					}
					if (resolvedProfile != null)
					{
						resolvedProfilesMap[$"{symbol}|{dataSource}"] = resolvedProfile;
					}
				}

				if (resolvedProfile == null)
				{
					continue;
				}

				found = true;

				var symbolKey = $"{resolvedProfile.Symbol}|{resolvedProfile.DataSource}";

				// First check if we already have a holding for this specific partial identifier
				if (FindHolding(partialIdentifierMap, partialIdentifiers, out Holding? existingHolding) && existingHolding != null)
				{
					logger.LogTrace("CreateOrReuseHolding: Merging identifiers for existing holding with symbol {Symbol}", resolvedProfile.Symbol);
					existingHolding.MergeSymbolProfiles(resolvedProfile);
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
					logger.LogTrace("CreateOrReuseHolding: Merging identifiers for existing holding with symbol {Symbol}", resolvedProfile.Symbol);
					existingHolding.MergeSymbolProfiles(resolvedProfile);
					existingHolding.MergeIdentifiers(partialIdentifiers);

					AddPartialIdentifiersToMap(partialIdentifierMap, partialIdentifiers, existingHolding);

					continue;
				}

				logger.LogDebug("CreateOrReuseHolding: Creating new holding for symbol {Symbol}", resolvedProfile.Symbol);

				// Try to reuse an existing holding, otherwise create a new one
				Holding holding;
				if (availableHoldings.TryDequeue(out Holding? reusedHolding))
				{
					logger.LogTrace("CreateOrReuseHolding: Reusing existing holding (ID: {HoldingId}) for symbol {Symbol}", reusedHolding.Id, resolvedProfile.Symbol);
					holding = reusedHolding;
				}
				else
				{
					logger.LogTrace("CreateOrReuseHolding: Creating new holding for symbol {Symbol}", resolvedProfile.Symbol);
					holding = new Holding();
					_ = databaseContext.Holdings.Add(holding);
				}

				holding.MergeSymbolProfiles(resolvedProfile);
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
			foreach (PartialSymbolIdentifier identifier in partialIdentifiers)
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
			foreach (PartialSymbolIdentifier identifier in partialIdentifiers)
			{
				if (partialIdentifierMap.TryGetValue(identifier, out existingHolding))
				{
					return true;
				}
			}

			return false;
		}

		// Efficient cache key using SHA256 hash
		private static string GetCacheKey(string prefix, Type matcherType, IEnumerable<PartialSymbolIdentifier> identifiers)
		{
			var sb = new StringBuilder();
			_ = sb.Append(prefix).Append('|').Append(matcherType.FullName).Append('|');
			foreach (PartialSymbolIdentifier? id in identifiers.OrderBy(x => x.Identifier).ThenBy(x => x.IdentifierType).ThenBy(x => x.Currency?.Symbol))
			{
				_ = sb.Append(id.Identifier).Append(':').Append(id.IdentifierType).Append(':').Append(id.Currency?.Symbol).Append('|');
			}
			using var sha = SHA256.Create();
			var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
			return prefix + "|" + matcherType.FullName + "|" + Convert.ToHexString(hash);
		}

		private IList<PartialSymbolIdentifier> GetIds(IList<PartialSymbolIdentifier> partialSymbolIdentifiers)
		{
			var ids = new List<PartialSymbolIdentifier>();
			foreach (PartialSymbolIdentifier partialIdentifier in partialSymbolIdentifiers)
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

		private static int GetIdentifierTypePriority(IdentifierType identifierType)
		{
			return identifierType switch
			{
				IdentifierType.Ticker => 0,
				IdentifierType.ISIN => 1,
				IdentifierType.Default => 2,
				IdentifierType.Name => 3,
				_ => 4,
			};
		}

		private int GetCurrencyPriority(Currency? currency)
		{
			return currency switch
			{
				null => 2,
				_ when currency.Symbol == primaryCurrency => 0,
				_ => 1,
			};
		}
	}
}