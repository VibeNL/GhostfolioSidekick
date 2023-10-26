using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
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
			IApplicationSettings applicationSettings)
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

			await DeleteUnusedSymbols();
			await AddManualSymbols();
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

				string trackingInsightSymbol = symbolConfiguration.TrackingInsightSymbol ?? string.Empty;
				if (marketData.Mappings.TrackInsight != trackingInsightSymbol)
				{
					marketData.Mappings.TrackInsight = trackingInsightSymbol;
					await api.UpdateMarketData(marketData);
				}
			}
		}

		private async Task DeleteUnusedSymbols()
		{
			var marketDataList = await api.GetMarketDataInfo();
			foreach (var marketData in marketDataList)
			{
				if (marketData.ActivitiesCount == 0)
				{
					await api.DeleteSymbol(marketData);
				}
			}
		}

		private async Task AddManualSymbols()
		{
			var symbolConfigurations = configurationInstance.Symbols;
			foreach (var symbolConfiguration in symbolConfigurations)
			{
				var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
				if (manualSymbolConfiguration == null)
				{
					continue;
				}

				var symbol = await api.FindSymbolByIdentifier(symbolConfiguration.Symbol);
				if (symbol == null)
				{
					await api.CreateManualSymbol(new Asset
					{
						AssetClass = manualSymbolConfiguration.AssetClass,
						AssetSubClass = manualSymbolConfiguration.AssetSubClass,
						Currency = CurrencyHelper.ParseCurrency(manualSymbolConfiguration.Currency),
						DataSource = "MANUAL",
						Name = manualSymbolConfiguration.Name,
						Symbol = symbolConfiguration.Symbol,
						ISIN = manualSymbolConfiguration.ISIN
					});
				}

				// TODO: update on difference???
			}
		}
	}
}