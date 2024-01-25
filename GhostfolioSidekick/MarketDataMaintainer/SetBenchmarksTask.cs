using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class SetBenchmarksTask : IScheduledWork
	{
		private readonly ILogger<SetTrackingInsightOnSymbolsTask> logger;
		private readonly IMarketDataService marketDataService;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.SetBenchmarks;

		public SetBenchmarksTask(
			ILogger<SetTrackingInsightOnSymbolsTask> logger,
			IMarketDataService marketDataManager,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.marketDataService = marketDataManager;
			this.applicationSettings = applicationSettings;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(SetBenchmarksTask)} Starting to do work");

			try
			{
				if (applicationSettings.ConfigurationInstance.Settings.DeleteUnusedSymbols)
				{
					await SetBenchmarks();
				}
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogInformation($"{nameof(SetBenchmarksTask)} Done");
		}

		private async Task SetBenchmarks()
		{
			var benchMarks = applicationSettings.ConfigurationInstance.Benchmarks;
			foreach (var symbolConfiguration in benchMarks ?? [])
			{
				var symbol = await marketDataService.FindSymbolByIdentifier([symbolConfiguration.Symbol], null, null, null, true, true);
				if (symbol != null)
				{
					await marketDataService.SetSymbolAsBenchmark(symbol, symbol.DataSource);
				}
			}
		}
	}
}
