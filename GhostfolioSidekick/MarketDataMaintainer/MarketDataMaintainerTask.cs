using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.MarketDataMaintainer.Actions;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class MarketDataMaintainerTask : IScheduledWork
	{
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;
		private readonly ConfigurationInstance configurationInstance;
		private int counter = -1;

		public int Priority => 9999;

		public MarketDataMaintainerTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.api = api ?? throw new ArgumentNullException(nameof(api));
			this.configurationInstance = applicationSettings.ConfigurationInstance;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(MarketDataMaintainerTask)} Starting to do work");

			api.ClearCache();

			await TryCatch(DeleteUnusedSymbols());
			await TryCatch(ManageManualSymbols());
			await TryCatch(SetTrackingInsightOnSymbols());
			await TryCatch(CreateBenchmarks());

			counter = (counter + 1) % 24; // HACK: once a day
			if (counter == 0)
			{
				await TryCatch(GatherAllData());
			}

			logger.LogInformation($"{nameof(MarketDataMaintainerTask)} Done");
		}

		private async Task GatherAllData()
		{
			// Bug Ghostfolio: Currencies are not updated until a new one is added.
			// Workaround: Adding and removing a dummy
			await api.AddAndRemoveDummyCurrency();
			await api.GatherAllMarktData();
		}

		private async Task SetTrackingInsightOnSymbols()
		{
			var marketDataInfoList = await api.GetMarketData();
			foreach (var marketDataInfo in marketDataInfoList)
			{
				var symbolConfiguration = configurationInstance.FindSymbol(marketDataInfo.AssetProfile.Symbol);
				if (symbolConfiguration == null)
				{
					continue;
				}

				var marketData = await api.GetMarketData(marketDataInfo.AssetProfile.Symbol, marketDataInfo.AssetProfile.DataSource);

				string trackingInsightSymbol = symbolConfiguration.TrackingInsightSymbol ?? string.Empty;
				if ((marketData.AssetProfile.Mappings.TrackInsight ?? string.Empty) != trackingInsightSymbol)
				{
					marketData.AssetProfile.Mappings.TrackInsight = trackingInsightSymbol;
					await api.UpdateSymbol(marketData.AssetProfile);
				}
			}
		}

		private async Task DeleteUnusedSymbols()
		{
			var marketDataList = await api.GetMarketData();
			foreach (var marketData in marketDataList)
			{
				if (marketData.AssetProfile.ActivitiesCount == 0)
				{
					await api.DeleteSymbol(marketData.AssetProfile);
				}
			}
		}

		private async Task ManageManualSymbols()
		{
			var action = new CreateManualSymbol(api, configurationInstance);
			await action.ManageManualSymbols();
		}

		private async Task CreateBenchmarks()
		{
			var benchMarks = configurationInstance.Benchmarks;
			foreach (var symbolConfiguration in benchMarks ?? [])
			{
				var symbol = await api.FindSymbolByIdentifier(symbolConfiguration.Symbol, null, null, null, true, true);
				if (symbol != null)
				{
					await api.SetSymbolAsBenchmark(symbol.Symbol, symbol.DataSource);
				}
			}
		}

		private async Task TryCatch(Task task)
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				logger.LogError(ex.Message);
			}
		}
	}
}