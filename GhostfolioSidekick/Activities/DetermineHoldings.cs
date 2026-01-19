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

		public TaskPriority Priority => TaskPriority.DetermineHoldings;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Determine Holdings";

		public async Task DoWork(ILogger logger)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var activities = await databaseContext.Activities.ToListAsync();

			// Load existing holdings to potentially reuse their IDs
			var existingHoldings = await databaseContext.Holdings.ToListAsync();
			var availableHoldings = new Queue<Holding>(existingHoldings);
			var usedHoldings = new List<Holding>();
			var symbolHoldingDictionary = new Dictionary<SymbolProfile, Holding>(new SymbolComparer());

			// Reset all existing holdings to a clean state
			foreach (var holding in existingHoldings)
			{
				holding.SymbolProfiles.Clear();
				holding.PartialSymbolIdentifiers.Clear();
				holding.Activities.Clear();
			}

			foreach (var partialIdentifiers in activities
					.OfType<IActivityWithPartialIdentifier>()
					.Select(x => x.PartialSymbolIdentifiers)
					.Distinct()
					.OrderBy(x => x[0].Identifier))
			{
				var ids = GetIds(partialIdentifiers);
				await CreateOrReuseHolding(logger, databaseContext, symbolHoldingDictionary, availableHoldings, usedHoldings, ids).ConfigureAwait(false);
			}

			// Remove any unused holdings
			var unusedHoldings = availableHoldings.ToList();
			if (unusedHoldings.Count > 0)
			{
				logger.LogInformation("Removing {Count} unused holdings", unusedHoldings.Count);
				databaseContext.Holdings.RemoveRange(unusedHoldings);
			}

			await databaseContext.SaveChangesAsync();
		}

		private async Task CreateOrReuseHolding(
			ILogger logger,
			DatabaseContext databaseContext,
			Dictionary<SymbolProfile, Holding> symbolHoldingDictionary,
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

					// Check if symbol profile already exists
					if (symbolProfile != null)
					{
						var existingSymbolProfile = await databaseContext.SymbolProfiles.FirstOrDefaultAsync(x => x.Symbol == symbolProfile.Symbol && x.DataSource == symbolProfile.DataSource).ConfigureAwait(false);
						symbolProfile = existingSymbolProfile ?? symbolProfile;
						memoryCache.Set(cacheKey, symbolProfile, CacheDuration.Short());
					}
					else
					{
						memoryCache.Set(cacheKey, symbolProfile, CacheDuration.Short());
					}
				}

				if (symbolProfile == null)
				{
					continue;
				}

				if (symbolHoldingDictionary.TryGetValue(symbolProfile, out var existingHolding))
				{
					logger.LogTrace("CreateOrReuseHolding: Merging identifiers for existing holding with symbol {Symbol}", symbolProfile.Symbol);
					existingHolding.MergeIdentifiers(partialIdentifiers);
					continue;
				}

				found = true;
				logger.LogTrace("CreateOrReuseHolding: Creating new holding for symbol {Symbol}", symbolProfile.Symbol);
				
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
				holding.SymbolProfiles.Add(symbolProfile);
				holding.MergeIdentifiers(partialIdentifiers);
				
				symbolHoldingDictionary.Add(symbolProfile, holding);
				usedHoldings.Add(holding);
			}

			if (!found)
			{
			logger.LogWarning("CreateOrReuseHolding: No symbol profile found for {PartialIdentifiers}", string.Join(", ", partialIdentifiers));
			}
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

			return [.. ids.DistinctBy(x => x.Identifier)];
		}
	}
}
