using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using GhostfolioSidekick.GhostfolioAPI.API;

namespace GhostfolioSidekick.Activities.Comparer
{
	public class DetermineHoldings(
			ILogger<DetermineHoldings> logger,
			ISymbolMatcher[] symbolMatchers,
			IDbContextFactory<DatabaseContext> databaseContextFactory,
			IMemoryCache memoryCache) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.DetermineHoldings;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public async Task DoWork()
		{
			using var databaseContext = databaseContextFactory.CreateDbContext();
			var activities = await databaseContext.Activities.ToListAsync();

			var currentHoldings = await databaseContext.Holdings.ToListAsync();
			var newHoldings = new List<Holding>();

			var symbolHoldingDictionary = new Dictionary<SymbolProfile, Holding>(new SymbolComparer());
			foreach (var partialIdentifiers in activities
					.OfType<IActivityWithPartialIdentifier>()
					.Select(x => x.PartialSymbolIdentifiers))
			{
				await CreateOrUpdateHolding(databaseContext, symbolHoldingDictionary, newHoldings, partialIdentifiers).ConfigureAwait(false);
			}

			// Remove holdings that are no longer relevant
			foreach (var holding in currentHoldings)
			{
				if (!newHoldings.Contains(holding, new HoldingComparer()))
				{
					logger.LogInformation($"Removing holding: {holding}");
					databaseContext.Holdings.Remove(holding);
				}
			}

			// Add or update holdings
			foreach (var holding in newHoldings)
			{
				if (currentHoldings.Contains(holding, new HoldingComparer()))
				{
					databaseContext.Holdings.Update(holding);
				}
				else
				{
					logger.LogInformation($"Adding holding: {holding}");
					databaseContext.Holdings.Add(holding);
				}
			}

			await databaseContext.SaveChangesAsync();
		}

        private async Task CreateOrUpdateHolding(DatabaseContext databaseContext, Dictionary<SymbolProfile, Holding> symbolHoldingDictionary, List<Holding> newHoldings, List<PartialSymbolIdentifier> partialIdentifiers)
        {
            var holding = newHoldings.FirstOrDefault(x => x.HasPartialSymbolIdentifier(partialIdentifiers));
            if (holding != null)
            {
                // Holding already exists
                logger.LogTrace("CreateOrUpdateHolding: Holding already exists for {PartialIdentifiers}", string.Join(", ", partialIdentifiers));
                return;
            }

            foreach (var symbolMatcher in symbolMatchers)
            {
                var cacheKey = nameof(DetermineHoldings) +  string.Join(",", partialIdentifiers);
                if (!memoryCache.TryGetValue<SymbolProfile>(cacheKey, out var symbolProfile))
                {
                    symbolProfile = await symbolMatcher.MatchSymbol(partialIdentifiers.ToArray()).ConfigureAwait(false);
					memoryCache.Set(cacheKey, symbolProfile, CacheDuration.Short());
                }

                if (symbolProfile == null)
                {
                    logger.LogTrace("CreateOrUpdateHolding: No symbol profile found for {PartialIdentifiers}", string.Join(", ", partialIdentifiers));
                    continue;
                }

                if (symbolHoldingDictionary.TryGetValue(symbolProfile, out holding))
                {
                    logger.LogTrace("CreateOrUpdateHolding: Holding already exists for {Symbol} with {PartialIdentifiers}", symbolProfile.Symbol, string.Join(", ", partialIdentifiers));
                    holding.MergeIdentifiers(partialIdentifiers);
                    continue;
                }

                holding = newHoldings.FirstOrDefault(x => x.HasPartialSymbolIdentifier(partialIdentifiers));
                if (holding == null)
                {
                    logger.LogTrace("CreateOrUpdateHolding: Creating new holding for {Symbol} with {PartialIdentifiers}", symbolProfile.Symbol, string.Join(", ", partialIdentifiers));
                    holding = new Holding();

                    // Check if symbol profile already exists
                    var existingSymbolProfile = await databaseContext.SymbolProfiles.FirstOrDefaultAsync(x => x.Symbol == symbolProfile.Symbol && x.DataSource == symbolProfile.DataSource).ConfigureAwait(false);
                    if (existingSymbolProfile != null)
                    {
                        symbolProfile = existingSymbolProfile;
                    }

                    holding.SymbolProfiles.Add(symbolProfile);
                    symbolHoldingDictionary.Add(symbolProfile, holding);

                    holding.MergeIdentifiers(partialIdentifiers);
                    newHoldings.Add(holding);
                }
                else
                {
                    logger.LogTrace("CreateOrUpdateHolding: Merging identifiers for {Symbol} with {PartialIdentifiers}", symbolProfile.Symbol, string.Join(", ", partialIdentifiers));
                    holding.MergeIdentifiers(partialIdentifiers);
                }
            }
        }
	}
}
