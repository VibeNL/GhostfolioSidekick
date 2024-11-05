using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using KellermanSoftware.CompareNetObjects;
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
				var minActivityDate = await databaseContext.Holdings.Where(x => x.SymbolProfiles.Contains(symbol))
					.SelectMany(x => x.Activities)
					.MinAsync(x => x!.Date);

				var date = DateOnly.FromDateTime(minActivityDate);
				var stockSplitRepository = stockPriceRepositories.SingleOrDefault(x => x.DataSource == symbol.DataSource);

				if (stockSplitRepository == null)
				{
					logger.LogWarning($"No stock price repository found for {symbol.DataSource}");
					continue;
				}

				var splits = await stockSplitRepository.GetStockSplits(symbol, date);

				var compareLogic = new CompareLogic() { Config = new ComparisonConfig { MaxDifferences = int.MaxValue, IgnoreObjectTypes = true, MembersToIgnore = ["Id"] } };

				foreach (var split in splits)
				{
					ComparisonResult result = compareLogic.Compare(splits, symbol.StockSplits);

					if (!result.AreEqual)
					{
						symbol.StockSplits = splits.ToList();
						symbol.MarketData.Clear();
					}
				}

				if (!await databaseContext.SymbolProfiles.ContainsAsync(symbol).ConfigureAwait(false))
				{
					await databaseContext.SymbolProfiles.AddAsync(symbol);
				}

				await databaseContext.SaveChangesAsync();
				logger.LogDebug($"Stock splits for {symbol.Symbol} from {symbol.DataSource} gathered. Found {splits.Count()}");
			}

		}
	}
}
