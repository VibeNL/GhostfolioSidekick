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
	public class DetermineHoldings : IScheduledWork
	{
		private readonly ILogger<DetermineHoldings> logger;
		private readonly ISymbolMatcher[] symbolMatchers;
		private readonly IDbContextFactory<DatabaseContext> databaseContextFactory;
		private readonly IMemoryCache memoryCache;
		private readonly Mapping[] mappings;

		public DetermineHoldings(
				ILogger<DetermineHoldings> logger,
				ISymbolMatcher[] symbolMatchers,
				IDbContextFactory<DatabaseContext> databaseContextFactory,
				IMemoryCache memoryCache,
				IApplicationSettings settings)
		{
			this.logger = logger;
			this.symbolMatchers = symbolMatchers;
			this.databaseContextFactory = databaseContextFactory;
			this.memoryCache = memoryCache;

			this.mappings = settings?.ConfigurationInstance?.Mappings ?? [];
		}

		public TaskPriority Priority => TaskPriority.DetermineHoldings;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			using var databaseContext = databaseContextFactory.CreateDbContext();
			var activities = await databaseContext.Activities.ToListAsync();

			var currentHoldings = await databaseContext.Holdings.ToListAsync();

			// Remove all symbolprofiles
			foreach (var holding in currentHoldings)
			{
				holding.SymbolProfiles.Clear();
			}

			var symbolHoldingDictionary = new Dictionary<SymbolProfile, Holding>(new SymbolComparer());
			foreach (var partialIdentifiers in activities
					.OfType<IActivityWithPartialIdentifier>()
					.Select(x => x.PartialSymbolIdentifiers)
					.OrderBy(x => x[0].Identifier))
			{
				var ids = GetIds(partialIdentifiers);
				await CreateOrUpdateHolding(databaseContext, symbolHoldingDictionary, currentHoldings, ids).ConfigureAwait(false);
			}

			// Remove holdings that are no longer relevant
			foreach (var holding in currentHoldings)
			{
				if (holding.SymbolProfiles.Count == 0)
				{
					logger.LogInformation("Removing holding without symbol profile. Holding ID: {HoldingId}, Holding Details: {Holding}", holding.Id, holding);
					databaseContext.Holdings.Remove(holding);
				}
			}

			await databaseContext.SaveChangesAsync();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		private async Task CreateOrUpdateHolding(DatabaseContext databaseContext, Dictionary<SymbolProfile, Holding> symbolHoldingDictionary, IList<Holding> holdings, IList<PartialSymbolIdentifier> partialIdentifiers)
		{
			var holding = holdings.FirstOrDefault(x => x.HasPartialSymbolIdentifier(partialIdentifiers));
			if (holding != null && holding.SymbolProfiles.Count != 0)
			{
				// Holding already exists
				logger.LogTrace("CreateOrUpdateHolding: Holding already exists for {PartialIdentifiers}", string.Join(", ", partialIdentifiers));
				return;
			}

			var found = false;
			foreach (var symbolMatcher in symbolMatchers)
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

				if (symbolHoldingDictionary.TryGetValue(symbolProfile, out holding))
				{
					logger.LogTrace("CreateOrUpdateHolding: Holding already exists for {Symbol} with {PartialIdentifiers}", symbolProfile.Symbol, string.Join(", ", partialIdentifiers));
					holding.MergeIdentifiers(partialIdentifiers);
					continue;
				}

				found = true;
				holding = holdings.FirstOrDefault(x => x.HasPartialSymbolIdentifier(partialIdentifiers));
				if (holding != null)
				{
					logger.LogTrace("CreateOrUpdateHolding: Holding already exists for {Symbol} with {PartialIdentifiers}", symbolProfile.Symbol, string.Join(", ", partialIdentifiers));
					holding.SymbolProfiles.Add(symbolProfile);
					holding.MergeIdentifiers(partialIdentifiers);
					symbolHoldingDictionary.Add(symbolProfile, holding);
					continue;
				}

				logger.LogTrace("CreateOrUpdateHolding: Creating new holding for {Symbol} with {PartialIdentifiers}", symbolProfile.Symbol, string.Join(", ", partialIdentifiers));
				holding = new Holding();

				holding.SymbolProfiles.Add(symbolProfile);
				symbolHoldingDictionary.Add(symbolProfile, holding);

				holding.MergeIdentifiers(partialIdentifiers);
				holdings.Add(holding);
				databaseContext.Holdings.Add(holding);
			}

			if (!found)
			{
				logger.LogWarning("CreateOrUpdateHolding: No symbol profile found for {PartialIdentifiers}", string.Join(", ", partialIdentifiers));
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
