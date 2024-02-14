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

			this.logger = logger;
			this.marketDataService = marketDataManager;
			this.applicationSettings = applicationSettings;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(SetTrackingInsightOnSymbolsTask)} Starting to do work");

			try
			{
				await SetTrackingInsightOnSymbols();
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
			var profiles = await marketDataService.GetAllSymbolProfiles();
			foreach (var profile in profiles)
			{
				var symbolConfiguration = applicationSettings.ConfigurationInstance.FindSymbol(profile.Symbol);
				if (symbolConfiguration == null)
				{
					continue;
				}

				var trackingInsightSymbol = symbolConfiguration.TrackingInsightSymbol ?? string.Empty;
				if ((profile.Mappings.TrackInsight ?? string.Empty) != trackingInsightSymbol)
				{
					profile.Mappings.TrackInsight = trackingInsightSymbol;
					await marketDataService.UpdateSymbol(profile);
				}
			}
		}
	}
}
