using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class SetBenchmarksTask : IScheduledWork
	{
		private readonly ILogger<SetBenchmarksTask> logger;
		private readonly IMarketDataService marketDataService;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.SetBenchmarks;
		
		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public SetBenchmarksTask(
			ILogger<SetBenchmarksTask> logger,
			IMarketDataService marketDataManager,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(logger);
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger;
			this.marketDataService = marketDataManager;
			this.applicationSettings = applicationSettings;
		}

		public async Task DoWork()
		{
			logger.LogDebug($"{nameof(SetBenchmarksTask)} Starting to do work");

			try
			{
				await SetBenchmarks();
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogDebug($"{nameof(SetBenchmarksTask)} Done");
		}

		private async Task SetBenchmarks()
		{
			var benchMarks = applicationSettings.ConfigurationInstance.Benchmarks;
			foreach (var symbolConfiguration in benchMarks ?? [])
			{
				var symbol = await marketDataService.FindSymbolByIdentifier([symbolConfiguration.Symbol], null, null, null, true, true);
				if (symbol != null)
				{
					await marketDataService.SetSymbolAsBenchmark(symbol);
				}
			}
		}
	}
}
