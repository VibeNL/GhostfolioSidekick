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

				if (symbol.MarketData.Count != 0)
				{
					var minDate = symbol.MarketData.Min(x => x.Date);
					var maxDate = symbol.MarketData.Max(x => x.Date);

					// Only get new data since our earliest date is inside the database
					// Or we cannot get data ealiers than we already have
					if (date >= minDate || minDate <= stockPriceRepository.MinDate)
					{
						date = maxDate;
					}

					// TODO
					if (maxDate >= DateOnly.FromDateTime(DateTime.Today))
					{
						// For now skip today
						logger.LogDebug($"Market data for {symbol.Symbol} from {symbol.DataSource} is up to date");
						continue;
					}
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
						databaseContext.MarketDatas.Remove(existingRecord);
						symbol.MarketData.Remove(existingRecord);
					}

					symbol.MarketData.Add(marketData);
				}

				await databaseContext.SaveChangesAsync();
				logger.LogDebug($"Market data for {symbol.Symbol} from {symbol.DataSource} gathered");
			}

		}
	}
}
