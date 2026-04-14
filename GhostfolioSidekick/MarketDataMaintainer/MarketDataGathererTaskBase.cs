using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal abstract class MarketDataGathererTaskBase(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IStockPriceRepository[] stockPriceRepositories) : IScheduledWork
	{
		public abstract TimeSpan ExecutionFrequency { get; }

		public bool ExceptionsAreFatal => false;

		public abstract string Name { get; }
		public abstract TaskPriority Priority { get; }

		protected abstract bool ShouldProcess(bool isCurrentlyOwned);

		public async Task DoWork(ILogger logger)
		{
			var symbolIdentifiers = new List<Tuple<string, string>>();
			using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				(await databaseContext.SymbolProfiles.Where(x => x.AssetClass != AssetClass.Undefined)
					.Select(x => new Tuple<string, string>(x.Symbol, x.DataSource))
					.ToListAsync())
					.OrderBy(x => x.Item1)
					.ThenBy(x => x.Item2)
					.ToList()
					.ForEach(symbolIdentifiers.Add);
			}

			foreach (var symbolIds in symbolIdentifiers)
			{
				logger.LogDebug("Gathering market data for {Symbol} from {DataSource}", symbolIds.Item1, symbolIds.Item2);

				using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
				var symbol = await databaseContext.SymbolProfiles
					.Include(x => x.MarketData)
					.Where(x => x.Symbol == symbolIds.Item1 && x.DataSource == symbolIds.Item2)
					.SingleAsync();
				var activities = databaseContext.Holdings.Where(x => x.SymbolProfiles.Contains(symbol))
					.SelectMany(x => x.Activities);

				if (!await activities.AnyAsync())
				{
					logger.LogDebug("No activities found for {Symbol} from {DataSource}", symbol.Symbol, symbol.DataSource);
					continue;
				}

				var isCurrentlyOwned = await IsCurrentlyOwned(databaseContext, symbol);
				if (!ShouldProcess(isCurrentlyOwned))
				{
					logger.LogDebug("Skipping market data for {Symbol} from {DataSource} (owned: {IsOwned})", symbol.Symbol, symbol.DataSource, isCurrentlyOwned);
					continue;
				}

				var minActivityDate = await activities.MinAsync(x => x.Date);

				var date = DateOnly.FromDateTime(minActivityDate);
				var stockPriceRepository = stockPriceRepositories.SingleOrDefault(x => x.DataSource == symbol.DataSource);

				if (stockPriceRepository == null)
				{
					continue;
				}

				// Determine the earliest date we should get the data for
				if (symbol.MarketData.Count != 0)
				{
					var minDate = symbol.MarketData.Min(x => x.Date);
					var maxDate = symbol.MarketData.Max(x => x.Date);

					// If any data corruption is detected, we will re-fetch all data if possible
					// For manual data sources, we always re-fetch all data
					var hasDataCorruption = symbol.MarketData.Any(x =>
						x.Close == 0 // Close price is not set
						|| x.IsGenerated
						) && symbol.MarketData.OrderBy(x => x.Date).Select(x => x.Close).LastOrDefault() != 0;
					if (hasDataCorruption)
					{
						logger.LogDebug("Data corruption detected / Manual datasource found for {Symbol} from {DataSource}. Re-fetching all data.", symbol.Symbol, symbol.DataSource);
					}
					// Only get new data since our earliest date is inside the database
					// Or we cannot get data ealier than we already have
					else
					{
						if (date >= minDate)
						{
							date = maxDate;
						}
					}
				}

				// If the repository does not support data before a certain date, set it to that date
				if (date < stockPriceRepository.MinDate)
				{
					date = stockPriceRepository.MinDate;
				}

				// Ensure the date is at least 7 days in the past
				var sevenDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
				if (date > sevenDaysAgo)
				{
					date = sevenDaysAgo;
				}

				var list = await stockPriceRepository.GetStockMarketData(symbol, date);

				foreach (var marketData in list)
				{
					var existingRecord = symbol.MarketData.SingleOrDefault(x => x.Date == marketData.Date);
					decimal closeAmount = marketData.Close;

					// Interpolate missing close prices
					if (closeAmount == 0)
					{
						var previous = symbol.MarketData
							.Where(x => x.Date < marketData.Date && x.Close != 0)
							.OrderByDescending(x => x.Date)
							.FirstOrDefault();
						var next = symbol.MarketData
							.Where(x => x.Date > marketData.Date && x.Close != 0)
							.OrderBy(x => x.Date)
							.FirstOrDefault();
						if (previous != null && next != null)
						{
							var a = previous.Date.ToDateTime(TimeOnly.MinValue);
							var b = marketData.Date.ToDateTime(TimeOnly.MinValue);
							var c = next.Date.ToDateTime(TimeOnly.MinValue);
							var total = (c - a).TotalDays;
							var prevWeight = (c - b).TotalDays / total;
							var nextWeight = (b - a).TotalDays / total;
							var weighted = previous.Close * (decimal)prevWeight + next.Close * (decimal)nextWeight;
							marketData.Close = weighted;
							marketData.IsGenerated = true;
						}
					}

					if (existingRecord != null && Math.Abs(existingRecord.Close - marketData.Close) < 0.00001m)
					{
						continue;
					}

					if (existingRecord != null)
					{
						existingRecord.CopyFrom(marketData);
					}
					else
					{
						symbol.MarketData.Add(marketData);
					}
				}

				await databaseContext.SaveChangesAsync();
				logger.LogDebug("Market data for {Symbol} from {DataSource} gathered", symbol.Symbol, symbol.DataSource);
			}
		}

		internal static async Task<bool> IsCurrentlyOwned(DatabaseContext databaseContext, SymbolProfile symbol)
		{
			var holdingIds = await databaseContext.Holdings
				.Where(h => h.SymbolProfiles.Contains(symbol))
				.Select(h => h.Id)
				.ToListAsync();

			if (holdingIds.Count == 0)
			{
				return false;
			}

			var latestSnapshots = new List<CalculatedSnapshot>();
			foreach (var holdingId in holdingIds)
			{
				var latestSnapshot = await databaseContext.CalculatedSnapshots
					.Where(cs => cs.HoldingId == holdingId)
					.OrderByDescending(cs => cs.Date)
					.FirstOrDefaultAsync();
				if (latestSnapshot != null)
				{
					latestSnapshots.Add(latestSnapshot);
				}
			}

			// If no snapshots exist, default to owned (safe - performance hasn't been calculated yet)
			if (latestSnapshots.Count == 0)
			{
				return true;
			}

			return latestSnapshots.Any(s => s.Quantity > 0);
		}
	}
}
