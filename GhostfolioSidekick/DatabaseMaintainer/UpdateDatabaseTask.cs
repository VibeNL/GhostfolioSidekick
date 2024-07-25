using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.DatabaseMaintainer
{
	internal class UpdateDatabaseTask(IMarketDataService marketDataService, IStockSplitRepository stockSplitRepository, ILogger<UpdateDatabaseTask> logger) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.PrepareDatabaseTask;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public async Task DoWork()
		{
			await new SyncSymbolsWithGhostfolio(marketDataService).Sync().ConfigureAwait(false);
			await new UpdateStockSplits(stockSplitRepository, logger).DoWork().ConfigureAwait(false);


		}
	}
}
