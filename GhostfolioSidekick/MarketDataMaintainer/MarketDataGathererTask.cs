using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataGathererTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IStockPriceRepository[] stockPriceRepositories, ILogger<MarketDataGathererTask> logger) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataGatherer;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		public async Task DoWork()
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
					.ForEach(x => symbolIdentifiers.Add(x));
			}

			foreach (var symbolIds in symbolIdentifiers)
			{
				logger.LogDebug($"Gathering market data for {symbolIds.Item1} from {symbolIds.Item2}");

				using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
				var symbol = await databaseContext.SymbolProfiles
					.Include(x => x.MarketData)
					.Where(x => x.Symbol == symbolIds.Item1 && x.DataSource == symbolIds.Item2)
					.SingleAsync();
				var activities = databaseContext.Holdings.Where(x => x.SymbolProfiles.Contains(symbol))
					.SelectMany(x => x.Activities);

				if (!await activities.AnyAsync())
				{
					logger.LogDebug($"No activities found for {symbol.Symbol} from {symbol.DataSource}");
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
					var hasDataCorruption = symbol.MarketData.Any(x =>
						x.Close.Amount == 0 // Close price is not set
						) && symbol.MarketData.OrderBy(x => x.Date).Select(x => x.Close.Amount).LastOrDefault() != 0;
					if (hasDataCorruption)
					{
						logger.LogWarning($"Data corruption detected for {symbol.Symbol} from {symbol.DataSource}. Re-fetching all data.");
					}
					// Only get new data since our earliest date is inside the database
					// Or we cannot get data ealiers than we already have
					else
					{
						if (date >= minDate)
						{
							date = maxDate;
						}

						// skip the current day
						if (maxDate >= DateOnly.FromDateTime(DateTime.Today))
						{
							// For now skip today
							logger.LogDebug($"Market data for {symbol.Symbol} from {symbol.DataSource} is up to date");
							continue;
						}
					}
				}

				// If the repository does not support data before a certain date, set it to that date
				if (date < stockPriceRepository.MinDate)
				{
					date = stockPriceRepository.MinDate;
				}

				var list = await stockPriceRepository.GetStockMarketData(symbol, date);

				foreach (var marketData in list)
				{
					var existingRecord = symbol.MarketData.SingleOrDefault(x => x.Date == marketData.Date);
					if (existingRecord != null && Math.Abs(existingRecord.Close.Amount - marketData.Close.Amount) < 0.00001m)
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
				logger.LogDebug($"Market data for {symbol.Symbol} from {symbol.DataSource} gathered");
			}

		}
	}
}
