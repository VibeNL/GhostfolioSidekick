using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataGathererTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IStockPriceRepository[] stockPriceRepositories, ILogger<MarketDataGathererTask> logger) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataGatherer;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

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
				var minActivityDate = await databaseContext.ActivitySymbols.Where(x => x.SymbolProfile == symbol)
					.MinAsync(x => x.Activity!.Date);

				var date = DateOnly.FromDateTime(minActivityDate);
				var stockPriceRepository = stockPriceRepositories.Single(x => x.DataSource == symbol.DataSource);

				if (symbol.MarketData.Count != 0)
				{
					var minDate = DateOnly.FromDateTime(symbol.MarketData.Min(x => x.Date));
					var maxDate = DateOnly.FromDateTime(symbol.MarketData.Max(x => x.Date));

					if (date >= minDate && DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) <= maxDate) // For now 1 day old only
					{
						logger.LogDebug($"Skipping {symbol.Symbol} as it is up to date");
						continue;
					}

					if (minDate <= stockPriceRepository.MinDate && DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) <= maxDate) // For now 1 day old only
					{
						logger.LogDebug($"Skipping {symbol.Symbol} as it is up to date");
						continue;
					}
				}

				var md = await stockPriceRepository.GetStockMarketData(symbol, date);

				symbol.MarketData.Clear();

				foreach (var marketData in md)
				{
					symbol.MarketData.Add(marketData);
				}

				if (!await databaseContext.SymbolProfiles.ContainsAsync(symbol).ConfigureAwait(false))
				{
					await databaseContext.SymbolProfiles.AddAsync(symbol);
				}

				await databaseContext.SaveChangesAsync();
				logger.LogDebug($"Market data for {symbol.Symbol} from {symbol.DataSource} gathered");
			}

		}
	}
}
