using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class GatherAllDataTask : IScheduledWork
	{
		private readonly ILogger<SetBenchmarksTask> logger;
		private readonly IMarketDataService marketDataService;
		private readonly IApplicationSettings applicationSettings;
		private int counter = -1;

		public TaskPriority Priority => TaskPriority.GatherAllData;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public GatherAllDataTask(
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
			logger.LogInformation($"{nameof(SetBenchmarksTask)} Starting to do work");
			try
			{
				counter = (counter + 1) % 24; // HACK: once a day
				if (counter == 0)
				{
					logger.LogInformation($"{nameof(SetBenchmarksTask)} Skipped");
					return;
				}

				await GatherAll();
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogInformation($"{nameof(SetBenchmarksTask)} Done");
		}

		private async Task GatherAll()
		{
			await marketDataService.GatherAllMarktData();
		}
	}
}
