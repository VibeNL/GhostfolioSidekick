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

		public TaskPriority Priority => TaskPriority.GatherAllData;

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
			// Bug Ghostfolio: Currencies are not updated until a new one is added.
			// Workaround: Adding and removing a dummy
			await marketDataService.AddAndRemoveDummyCurrency();
			await marketDataService.GatherAllMarktData();
		}
	}
}
