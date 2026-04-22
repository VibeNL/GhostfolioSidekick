using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataGathererTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IStockPriceRepository[] stockPriceRepositories) : IScheduledWork
	{
		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public TaskPriority Priority => TaskPriority.MarketDataGatherer;

		public string Name => "Market Data Gatherer";

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
					var hasDataCorruption = symbol.MarketData.Any(x => x.Close == 0 || x.IsGenerated)
						&& symbol.MarketData.OrderBy(x => x.Date).Select(x => x.Close).LastOrDefault() != 0;
					if (hasDataCorruption)
					{
						logger.LogDebug("Data corruption detected / Manual datasource found for {Symbol} from {DataSource}. Re-fetching all data.", symbol.Symbol, symbol.DataSource);
					}
					else
					{
						if (date >= minDate)
						{
							date = maxDate;
						}
					}
				}

				var lastTradingDay = GetLastTradingDay();
				var dataIsUpToDate = symbol.MarketData.Count != 0 && symbol.MarketData.Max(x => x.Date) >= lastTradingDay;
				if (dataIsUpToDate && !await IsCurrentlyOwned(databaseContext, symbol))
				{
					logger.LogDebug("{Symbol} from {DataSource} is not currently owned and data is up to date (latest >= {LastTradingDay}). Skipping till next trading day", symbol.Symbol, symbol.DataSource, lastTradingDay);
					continue;
				}

				if (date < stockPriceRepository.MinDate)
				{
					date = stockPriceRepository.MinDate;
				}

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
					if (closeAmount == 0)
					{
						var previous = symbol.MarketData.Where(x => x.Date < marketData.Date && x.Close != 0).OrderByDescending(x => x.Date).FirstOrDefault();
						var next = symbol.MarketData.Where(x => x.Date > marketData.Date && x.Close != 0).OrderBy(x => x.Date).FirstOrDefault();
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

				// Fill all missing dates between first activity and last trading day
				var mainHolding = databaseContext.Holdings.Include(h => h.Activities).FirstOrDefault(h => h.SymbolProfiles.Contains(symbol));
				if (mainHolding == null || !mainHolding.Activities.Any())
				{
					continue;
				}
				var mainBuy = mainHolding.Activities.OfType<GhostfolioSidekick.Model.Activities.Types.BuyActivity>().OrderBy(a => a.Date).FirstOrDefault();
				var mainSell = mainHolding.Activities.OfType<GhostfolioSidekick.Model.Activities.Types.SellActivity>().OrderBy(a => a.Date).FirstOrDefault();
				if (mainBuy == null)
				{
					continue;
				}
				var gapFillMinDate = DateOnly.FromDateTime(mainBuy.Date);
				var gapFillMaxDate = mainSell != null ? DateOnly.FromDateTime(mainSell.Date) : GetLastTradingDay();
				for (var d = gapFillMinDate; d <= gapFillMaxDate; d = d.AddDays(1))
				{
					foreach (var md in symbol.MarketData.Where(md => md.Date == d).ToList())
					{
						symbol.MarketData.Remove(md);
					}
					var previous = symbol.MarketData.Where(x => x.Date < d && x.Close != 0).OrderByDescending(x => x.Date).FirstOrDefault();
					var next = symbol.MarketData.Where(x => x.Date > d && x.Close != 0).OrderBy(x => x.Date).FirstOrDefault();
					if (previous != null && next != null)
					{
						var a = previous.Date.ToDateTime(TimeOnly.MinValue);
						var b = d.ToDateTime(TimeOnly.MinValue);
						var c = next.Date.ToDateTime(TimeOnly.MinValue);
						var total = (c - a).TotalDays;
						var prevWeight = (c - b).TotalDays / total;
						var nextWeight = (b - a).TotalDays / total;
						var weighted = previous.Close * (decimal)prevWeight + next.Close * (decimal)nextWeight;
						symbol.MarketData.Add(new GhostfolioSidekick.Model.Market.MarketData
						{
							Currency = previous.Currency,
							Close = weighted,
							Open = weighted,
							High = weighted,
							Low = weighted,
							TradingVolume = 0,
							Date = d,
							IsGenerated = true
						});
					}
					else
					{
						var gapFillBuy = mainHolding.Activities.OfType<GhostfolioSidekick.Model.Activities.Types.BuyActivity>().OrderBy(a => a.Date).FirstOrDefault();
						var gapFillSell = mainHolding.Activities.OfType<GhostfolioSidekick.Model.Activities.Types.SellActivity>().OrderBy(a => a.Date).FirstOrDefault();
						if (gapFillBuy != null && (gapFillSell == null || d < DateOnly.FromDateTime(gapFillSell.Date)))
						{
							symbol.MarketData.Add(new GhostfolioSidekick.Model.Market.MarketData
							{
								Currency = gapFillBuy.UnitPrice.Currency,
								Close = gapFillBuy.UnitPrice.Amount,
								Open = gapFillBuy.UnitPrice.Amount,
								High = gapFillBuy.UnitPrice.Amount,
								Low = gapFillBuy.UnitPrice.Amount,
								TradingVolume = 0,
								Date = d,
								IsGenerated = true
							});
						}
						else if (gapFillSell != null && d >= DateOnly.FromDateTime(gapFillSell.Date))
						{
							symbol.MarketData.Add(new GhostfolioSidekick.Model.Market.MarketData
							{
								Currency = gapFillSell.UnitPrice.Currency,
								Close = gapFillSell.UnitPrice.Amount,
								Open = gapFillSell.UnitPrice.Amount,
								High = gapFillSell.UnitPrice.Amount,
								Low = gapFillSell.UnitPrice.Amount,
								TradingVolume = 0,
								Date = d,
								IsGenerated = true
							});
						}
					}
				}
				await databaseContext.SaveChangesAsync();
				logger.LogDebug("Market data for {Symbol} from {DataSource} gathered", symbol.Symbol, symbol.DataSource);
			}
		}

		internal static DateOnly GetLastTradingDay()
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			return today.DayOfWeek switch
			{
				DayOfWeek.Sunday => today.AddDays(-2),
				DayOfWeek.Saturday => today.AddDays(-1),
				_ => today
			};
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
