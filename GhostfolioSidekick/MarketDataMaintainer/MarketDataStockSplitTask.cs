using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataStockSplitTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IStockSplitRepository[] stockPriceRepositories, ILogger<MarketDataGathererTask> logger) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataStockSplit;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			var symbolIdentifiers = new List<Tuple<string, string>>();
			using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
#pragma warning disable S2971 // LINQ expressions should be simplified
				(await databaseContext.SymbolProfiles.Where(x => x.AssetSubClass == AssetSubClass.Stock)
					.Select(x => new Tuple<string, string>(x.Symbol, x.DataSource))
					.ToListAsync())
					.OrderBy(x => x.Item1)
					.ThenBy(x => x.Item2)
					.ToList()
					.Where(x => !Datasource.IsGhostfolio(x.Item2))
					.ToList()
					.ForEach(x => symbolIdentifiers.Add(x));
#pragma warning restore S2971 // LINQ expressions should be simplified
			}

			foreach (var symbolIds in symbolIdentifiers)
			{
				using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
				var symbol = await databaseContext.SymbolProfiles
					.Include(x => x.MarketData)
					.Where(x => x.Symbol == symbolIds.Item1 && x.DataSource == symbolIds.Item2)
					.SingleAsync();

				var activities = databaseContext.Holdings.Where(x => x.SymbolProfiles.Contains(symbol))
					.SelectMany(x => x.Activities);

				if (!await activities.AnyAsync())
				{
					logger.LogTrace($"No activities found for {symbol.Symbol} from {symbol.DataSource}");
					continue;
				}

				var minActivityDate = await activities.MinAsync(x => x!.Date);

				var date = DateOnly.FromDateTime(minActivityDate);
				var stockSplitRepository = stockPriceRepositories.SingleOrDefault(x => x.DataSource == symbol.DataSource);

				if (stockSplitRepository == null)
				{
					logger.LogWarning($"No stock split repository found for {symbol.DataSource}");
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

				if (splits.Any())
				{
					logger.LogDebug($"Stock splits for {symbol.Symbol} from {symbol.DataSource} gathered. Found {splits.Count()}");
				}
			}

		}
	}
}
