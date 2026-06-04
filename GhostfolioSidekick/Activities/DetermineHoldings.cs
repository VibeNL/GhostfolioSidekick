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
			List<int> usedHoldingIds;
			using (DatabaseContext databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				List<Activity> activities = await databaseContext.Activities.ToListAsync();
				List<Holding> existingHoldings = await databaseContext.Holdings
					.Include(h => h.SymbolProfiles)
					.Include(h => h.Activities)
					.AsSplitQuery()
					.ToListAsync();

				Dictionary<PartialSymbolIdentifier, DesiredHoldingState> partialIdentifierMap = new(new PartialSymbolIdentifierComparer());
				Dictionary<string, DesiredHoldingState> symbolProfileMap = [];
				Dictionary<string, SymbolProfile> resolvedProfilesMap = [];
				HashSet<Activity> assignedActivities = new(ReferenceEqualityComparer.Instance);
				List<DesiredHoldingState> desiredHoldings = [];

				foreach (Activity activity in activities
					.Where(x => x is IActivityWithPartialIdentifier)
					.OrderByDescending(x => x.Date)
					.ThenByDescending(x => x.SortingPriority ?? int.MinValue)
					.ThenByDescending(x => x.Id))
				{
					if (activity is not IActivityWithPartialIdentifier activityWithPartialIdentifier)
					{
						continue;
					}

					List<PartialSymbolIdentifier> partialIdentifiers = activityWithPartialIdentifier.PartialSymbolIdentifiers;
					if (partialIdentifiers == null || partialIdentifiers.Count == 0)
					{
						logger.LogWarning("DetermineHoldings: Activity {ActivityType} has null or empty PartialSymbolIdentifiers. Activity details: {@Activity}", activity.GetType().Name, activity);
						UnassignActivity(activity);
						continue;
					}

					IList<PartialSymbolIdentifier> ids = GetIds(partialIdentifiers);
					SymbolProfile? resolvedProfile = await ResolveSymbolProfile(databaseContext, resolvedProfilesMap, ids).ConfigureAwait(false);
					if (resolvedProfile == null)
					{
						logger.LogWarning("CreateOrReuseHolding: No symbol profile found for {PartialIdentifiers}", string.Join(", ", ids));
						UnassignActivity(activity);
						continue;
					}

					DesiredHoldingState desiredHolding = GetOrCreateHolding(desiredHoldings, partialIdentifierMap, symbolProfileMap, ids, resolvedProfile);
					desiredHolding.MergeSymbolProfile(resolvedProfile);
					desiredHolding.MergeIdentifiers(ids);
					desiredHolding.AddActivity(activity);
					assignedActivities.Add(activity);

					AddPartialIdentifiersToMap(partialIdentifierMap, ids, desiredHolding);
					string symbolKey = GetSymbolKey(resolvedProfile.Symbol, resolvedProfile.DataSource);
					if (!symbolProfileMap.ContainsKey(symbolKey))
					{
						symbolProfileMap[symbolKey] = desiredHolding;
					}
				}

				foreach (Activity activity in activities.Where(x => x is IActivityWithPartialIdentifier && !assignedActivities.Contains(x)))
				{
					UnassignActivity(activity);
				}

				List<Holding> unusedHoldings = [.. existingHoldings];
				List<Holding> usedHoldings = [];
				foreach (DesiredHoldingState desiredHolding in desiredHoldings)
				{
					Holding? reusableHolding = FindReusableHolding(unusedHoldings, desiredHolding);
					Holding holding = reusableHolding ?? new Holding();
					if (reusableHolding == null)
					{
						logger.LogTrace("CreateOrReuseHolding: Creating new holding for symbol {Symbol}", desiredHolding.SymbolProfiles.First().Symbol);
						_ = databaseContext.Holdings.Add(holding);
					}
					else
					{
						logger.LogTrace("CreateOrReuseHolding: Reusing existing holding (ID: {HoldingId}) for symbol {Symbol}", holding.Id, desiredHolding.SymbolProfiles.First().Symbol);
						_ = unusedHoldings.Remove(holding);
					}

					ApplyHoldingState(holding, desiredHolding);
					usedHoldings.Add(holding);
				}

				if (unusedHoldings.Count > 0)
				{
					logger.LogInformation("Removing {Count} unused holdings", unusedHoldings.Count);
					databaseContext.Holdings.RemoveRange(unusedHoldings);
				}

				_ = await databaseContext.SaveChangesAsync();
				usedHoldingIds = [.. usedHoldings.Where(h => h.Id != 0).Select(h => h.Id)];
			}

			using DatabaseContext matchingContext = await databaseContextFactory.CreateDbContextAsync();
			List<Holding> holdingsForMatching = await matchingContext.Holdings
				.Include(h => h.SymbolProfiles)
				.Where(h => usedHoldingIds.Contains(h.Id))
				.ToListAsync();
			await MatchOtherMatchers(logger, matchingContext, holdingsForMatching).ConfigureAwait(false);
		}

		private async Task MatchOtherMatchers(ILogger logger, DatabaseContext databaseContext, List<Holding> usedHoldings)
		{
			Dictionary<string, SymbolProfile> resolvedProfilesMap = [];
			foreach (Holding holding in usedHoldings)
			{
				foreach (ISymbolMatcher symbolMatcher in symbolMatchers.Where(x => !x.AllowedForDeterminingHolding))
				{
					string cacheKey = $"{nameof(DetermineHoldings)}|{symbolMatcher.GetType()}|{string.Join(",", holding.PartialSymbolIdentifiers)}";
					if (!memoryCache.TryGetValue<(string Symbol, string DataSource)>(cacheKey, out (string Symbol, string DataSource) symbolProfileKey))
					{
						PartialSymbolIdentifier[] orderedIdentifiers = [.. holding.PartialSymbolIdentifiers
							.OrderBy(x => GetIdentifierTypePriority(x.IdentifierType))
							.ThenBy(x => GetCurrencyPriority(x.Currency))];
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
						if (!resolvedProfilesMap.TryGetValue(GetSymbolKey(symbol, dataSource), out SymbolProfile? resolvedProfile))
						{
							SymbolProfile? existing = await databaseContext.SymbolProfiles.FindAsync(symbol, dataSource).ConfigureAwait(false);
							if (existing != null)
							{
								resolvedProfile = existing;
							}
							else
							{
								PartialSymbolIdentifier[] orderedIdentifiers = [.. holding.PartialSymbolIdentifiers
									.OrderBy(x => GetIdentifierTypePriority(x.IdentifierType))
									.ThenBy(x => GetCurrencyPriority(x.Currency))];
								resolvedProfile = await symbolMatcher.MatchSymbol(orderedIdentifiers).ConfigureAwait(false);
								if (resolvedProfile != null)
								{
									_ = databaseContext.SymbolProfiles.Add(resolvedProfile);
								}
							}

							if (resolvedProfile != null)
							{
								resolvedProfilesMap[GetSymbolKey(symbol, dataSource)] = resolvedProfile;
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

		private async Task<SymbolProfile?> ResolveSymbolProfile(DatabaseContext databaseContext, Dictionary<string, SymbolProfile> resolvedProfilesMap, IList<PartialSymbolIdentifier> partialIdentifiers)
		{
			foreach (ISymbolMatcher symbolMatcher in symbolMatchers.Where(x => x.AllowedForDeterminingHolding))
			{
				string cacheKey = $"{nameof(DetermineHoldings)}|{symbolMatcher.GetType()}|{string.Join(",", partialIdentifiers)}";
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
				string symbolKey = GetSymbolKey(symbol, dataSource);
				if (resolvedProfilesMap.TryGetValue(symbolKey, out SymbolProfile? resolvedProfile))
				{
					return resolvedProfile;
				}

				SymbolProfile? existing = await databaseContext.SymbolProfiles.FindAsync(symbol, dataSource).ConfigureAwait(false);
				if (existing != null)
				{
					resolvedProfilesMap[symbolKey] = existing;
					return existing;
				}

				SymbolProfile? matchedProfile = await symbolMatcher.MatchSymbol([.. partialIdentifiers]).ConfigureAwait(false);
				if (matchedProfile != null)
				{
					_ = databaseContext.SymbolProfiles.Add(matchedProfile);
					resolvedProfilesMap[symbolKey] = matchedProfile;
					return matchedProfile;
				}
			}

			return null;
		}

		private static DesiredHoldingState GetOrCreateHolding(
			List<DesiredHoldingState> desiredHoldings,
			Dictionary<PartialSymbolIdentifier, DesiredHoldingState> partialIdentifierMap,
			Dictionary<string, DesiredHoldingState> symbolProfileMap,
			IList<PartialSymbolIdentifier> partialIdentifiers,
			SymbolProfile resolvedProfile)
		{
			if (FindHolding(partialIdentifierMap, partialIdentifiers, out DesiredHoldingState? existingHolding) && existingHolding != null)
			{
				return existingHolding;
			}

			string symbolKey = GetSymbolKey(resolvedProfile.Symbol, resolvedProfile.DataSource);
			if (symbolProfileMap.TryGetValue(symbolKey, out existingHolding))
			{
				return existingHolding;
			}

			DesiredHoldingState desiredHolding = new();
			desiredHoldings.Add(desiredHolding);
			return desiredHolding;
		}

		private static Holding? FindReusableHolding(List<Holding> unusedHoldings, DesiredHoldingState desiredHolding)
		{
			Holding? byIdentifier = unusedHoldings.FirstOrDefault(holding => desiredHolding.PartialSymbolIdentifiers.Any(holding.IdentifierContainsInList));
			if (byIdentifier != null)
			{
				return byIdentifier;
			}

			HashSet<string> desiredSymbolKeys = [.. desiredHolding.SymbolProfiles.Select(x => GetSymbolKey(x.Symbol, x.DataSource))];
			return unusedHoldings.FirstOrDefault(holding => holding.SymbolProfiles.Any(x => desiredSymbolKeys.Contains(GetSymbolKey(x.Symbol, x.DataSource))));
		}

		private static void ApplyHoldingState(Holding holding, DesiredHoldingState desiredHolding)
		{
			foreach (Activity existingActivity in holding.Activities.ToList())
			{
				if (!desiredHolding.Activities.Contains(existingActivity))
				{
					existingActivity.Holding = null;
				}
			}

			holding.SymbolProfiles.Clear();
			holding.PartialSymbolIdentifiers.Clear();
			holding.Activities.Clear();

			foreach (SymbolProfile symbolProfile in desiredHolding.SymbolProfiles)
			{
				holding.MergeSymbolProfiles(symbolProfile);
			}

			foreach (PartialSymbolIdentifier partialIdentifier in desiredHolding.PartialSymbolIdentifiers)
			{
				holding.MergeIdentifiers([partialIdentifier]);
			}

			foreach (Activity activity in desiredHolding.Activities)
			{
				if (activity.Holding != null && !ReferenceEquals(activity.Holding, holding))
				{
					_ = activity.Holding.Activities.Remove(activity);
				}

				activity.Holding = holding;
				if (!holding.Activities.Contains(activity))
				{
					holding.Activities.Add(activity);
				}
			}
		}

		private static void UnassignActivity(Activity activity)
		{
			if (activity.Holding != null)
			{
				_ = activity.Holding.Activities.Remove(activity);
			}

			activity.Holding = null;
		}

		private static string GetSymbolKey(string? symbol, string? dataSource)
		{
			return $"{symbol}|{dataSource}";
		}

		private static void AddPartialIdentifiersToMap(
			Dictionary<PartialSymbolIdentifier, DesiredHoldingState> partialIdentifierMap,
			IList<PartialSymbolIdentifier> partialIdentifiers,
			DesiredHoldingState holding)
		{
			foreach (PartialSymbolIdentifier identifier in partialIdentifiers)
			{
				if (!partialIdentifierMap.ContainsKey(identifier))
				{
					partialIdentifierMap[identifier] = holding;
				}
			}
		}

		private static bool FindHolding(
			Dictionary<PartialSymbolIdentifier, DesiredHoldingState> partialIdentifierMap,
			IList<PartialSymbolIdentifier> partialIdentifiers,
			out DesiredHoldingState? existingHolding)
		{
			existingHolding = null;
			foreach (PartialSymbolIdentifier identifier in partialIdentifiers)
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
			List<PartialSymbolIdentifier> ids = [];
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

		private sealed class DesiredHoldingState
		{
			public List<SymbolProfile> SymbolProfiles { get; } = [];

			public List<Activity> Activities { get; } = [];

			public List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; } = [];

			public void AddActivity(Activity activity)
			{
				if (!Activities.Contains(activity))
				{
					Activities.Add(activity);
				}
			}

			public void MergeIdentifiers(IEnumerable<PartialSymbolIdentifier> identifiers)
			{
				foreach (PartialSymbolIdentifier identifier in identifiers)
				{
					if (!PartialSymbolIdentifiers.Any(existing => existing.Equals(identifier)))
					{
						PartialSymbolIdentifiers.Add(identifier);
					}
				}
			}

			public void MergeSymbolProfile(SymbolProfile symbolProfile)
			{
				if (!SymbolProfiles.Any(existing => existing.Symbol == symbolProfile.Symbol && existing.DataSource == symbolProfile.DataSource))
				{
					SymbolProfiles.Add(symbolProfile);
				}
			}
		}
	}
}
