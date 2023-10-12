﻿using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class MarketDataMaintainerTask : IScheduledWork
	{
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;

		public MarketDataMaintainerTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.api = api ?? throw new ArgumentNullException(nameof(api));
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
			var marketDataList = await api.GetMarketDataInfo();
			foreach (var marketData in marketDataList)
			{

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