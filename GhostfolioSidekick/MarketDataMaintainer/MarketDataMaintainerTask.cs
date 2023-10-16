﻿using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class MarketDataMaintainerTask : IScheduledWork
	{
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;
		private readonly ConfigurationInstance configurationInstance;

		public MarketDataMaintainerTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api,
			ApplicationSettings applicationSettings)
		{
			if (applicationSettings is null)
			{
				throw new ArgumentNullException(nameof(applicationSettings));
			}

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.api = api ?? throw new ArgumentNullException(nameof(api));
			this.configurationInstance = applicationSettings.ConfigurationInstance;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(MarketDataMaintainerTask)} Starting to do work");

			await DeletUnusedSymbols();
			await SetTrackingInsightOnSymbols();

			logger.LogInformation($"{nameof(MarketDataMaintainerTask)} Done");
		}

		private async Task SetTrackingInsightOnSymbols()
		{
			var marketDataInfoList = await api.GetMarketDataInfo();
			foreach (var marketDataInfo in marketDataInfoList)
			{
				var symbolConfiguration = configurationInstance.FindSymbol(marketDataInfo.Symbol);
				if (symbolConfiguration == null)
				{
					continue;
				}

				var marketData = await api.GetMarketData(marketDataInfo);

				if (marketData.Mappings.TrackInsight != symbolConfiguration.TrackingInsightSymbol)
				{
					marketData.Mappings.TrackInsight = symbolConfiguration.TrackingInsightSymbol;
					await api.UpdateMarketData(marketData);
				}
			}
		}

		private async Task DeletUnusedSymbols()
		{
			var marketDataList = await api.GetMarketDataInfo();
			foreach (var marketData in marketDataList)
			{
				if (marketData.ActivitiesCount == 0)
				{
					await api.DeleteMarketData(marketData);
				}
			}
		}
	}
}