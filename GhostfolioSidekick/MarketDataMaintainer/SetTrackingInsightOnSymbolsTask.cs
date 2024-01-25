using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class SetTrackingInsightOnSymbolsTask : IScheduledWork
	{
		private readonly ILogger<SetTrackingInsightOnSymbolsTask> logger;
		private readonly IMarketDataService marketDataService;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.SetTrackingInsightOnSymbols;

		public SetTrackingInsightOnSymbolsTask(
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
			logger.LogInformation($"{nameof(SetTrackingInsightOnSymbolsTask)} Starting to do work");

			try
			{
				if (applicationSettings.ConfigurationInstance.Settings.DeleteUnusedSymbols)
				{
					await SetTrackingInsightOnSymbols();
				}
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogInformation($"{nameof(SetTrackingInsightOnSymbolsTask)} Done");
		}

		private async Task SetTrackingInsightOnSymbols()
		{
			var marketDataInfoList = await marketDataService.GetMarketData();
			foreach (var marketDataInfo in marketDataInfoList)
			{
				var symbolConfiguration = applicationSettings.ConfigurationInstance.FindSymbol(marketDataInfo.AssetProfile.Symbol);
				if (symbolConfiguration == null)
				{
					continue;
				}

				var trackingInsightSymbol = symbolConfiguration.TrackingInsightSymbol ?? string.Empty;
				if ((marketDataInfo.AssetProfile.Mappings.TrackInsight ?? string.Empty) != trackingInsightSymbol)
				{
					marketDataInfo.AssetProfile.Mappings.TrackInsight = trackingInsightSymbol;
					await marketDataService.UpdateSymbol(marketDataInfo.AssetProfile);
				}
			}
		}
	}
}
