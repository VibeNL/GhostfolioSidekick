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
	internal class MarketDataStockSplitTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IStockSplitRepository[] stockPriceRepositories, ILogger<MarketDataGathererTask> logger) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataStockSplit;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			var symbolIdentifiers = new List<Tuple<string, string>>();
			using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				(await databaseContext.SymbolProfiles.Where(x => x.AssetSubClass == AssetSubClass.Stock)
					.Select(x => new Tuple<string, string>(x.Symbol, x.DataSource))
					.ToListAsync())
					.OrderBy(x => x.Item1)
					.ThenBy(x => x.Item2)
					.ToList()
					.ForEach(x => symbolIdentifiers.Add(x));
			}

			foreach (var symbolIds in symbolIdentifiers)
			{
				logger.LogDebug($"Gathering stock split data for {symbolIds.Item1} from {symbolIds.Item2}");

				using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
				var symbol = await databaseContext.SymbolProfiles
					.Include(x => x.MarketData)
					.Where(x => x.Symbol == symbolIds.Item1 && x.DataSource == symbolIds.Item2)
					.SingleAsync();
				var minActivityDate = await databaseContext.ActivitySymbols.Where(x => x.SymbolProfile == symbol)
					.MinAsync(x => x.Activity!.Date);

				var date = DateOnly.FromDateTime(minActivityDate);
				var stockSplitRepository = stockPriceRepositories.SingleOrDefault(x => x.DataSource == symbol.DataSource);

				if (stockSplitRepository == null)
				{
					logger.LogWarning($"No stock price repository found for {symbol.DataSource}");
					continue;
				}

				var splits = await stockSplitRepository.GetStockSplits(symbol, date);

				symbol.StockSplits.Clear();

				foreach (var split in splits)
				{
					symbol.StockSplits.Add(split);
				}

				if (!await databaseContext.SymbolProfiles.ContainsAsync(symbol).ConfigureAwait(false))
				{
					await databaseContext.SymbolProfiles.AddAsync(symbol);
				}

				// TODO remove data before the split date if it is new
				

				await databaseContext.SaveChangesAsync();
				logger.LogDebug($"Stock splits for {symbol.Symbol} from {symbol.DataSource} gathered");
			}

		}
	}
}
