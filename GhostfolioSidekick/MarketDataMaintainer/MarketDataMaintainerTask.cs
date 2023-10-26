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
			await ManageManualSymbols();
			await SetTrackingInsightOnSymbols();

			logger.LogInformation($"{nameof(MarketDataMaintainerTask)} Done");
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
					await api.UpdateMarketData(marketData.AssetProfile);
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
			var marketData = (await api.GetMarketData()).ToList();
			var activities = (await api.GetAllActivities()).ToList();

			var symbolConfigurations = configurationInstance.Symbols;
			foreach (var symbolConfiguration in symbolConfigurations)
			{
				var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
				if (manualSymbolConfiguration == null)
				{
					continue;
				}

				await AddOrUpdateSymbol(symbolConfiguration, manualSymbolConfiguration);
				await SetKnownPrices(symbolConfiguration, marketData, activities);
			}
		}

		private async Task SetKnownPrices(SymbolConfiguration symbolConfiguration, List<Model.MarketDataList> marketData, List<Model.Activity> activities)
		{
			var mdi = marketData.SingleOrDefault(x => x.AssetProfile.Symbol == symbolConfiguration.Symbol);
			if (mdi == null || mdi.AssetProfile.ActivitiesCount <= 0)
			{
				return;
			}

			var md = await api.GetMarketData(mdi.AssetProfile.Symbol, mdi.AssetProfile.DataSource);
			var activitiesForSymbol = activities.Where(x => x.Asset?.Symbol == mdi.AssetProfile.Symbol).ToList();

			if (!activitiesForSymbol.Any())
			{
				return;
			}

			var sortedActivities = activitiesForSymbol.OrderBy(x => x.Date).ToList();

			for (var i = 0; i < sortedActivities.Count(); i++)
			{
				var fromActivity = sortedActivities[i];
				Activity toActivity;

				if (i + 1 < sortedActivities.Count())
				{
					toActivity = sortedActivities[i + 1];
				}
				else
				{
					toActivity = new Activity { Date = DateTime.Today.AddDays(1), UnitPrice = fromActivity.UnitPrice };
				}

				for (var date = fromActivity.Date; date <= toActivity.Date; date = date.AddDays(1))
				{
					var a = (decimal)(date - fromActivity.Date).TotalDays;
					var b = (decimal)(toActivity.Date - date).TotalDays;
					var percentage = a / (a + b);
					decimal amountFrom = fromActivity.UnitPrice.Amount;
					decimal amountTo = toActivity.UnitPrice.Amount;
					var expectedPrice = amountFrom + (percentage * (amountTo - amountFrom));

					var price = md.MarketData.FirstOrDefault(x => x.Date.Date == date.Date);

					if (price?.MarketPrice != expectedPrice)
					{
						await api.SetMarketPrice(md.AssetProfile, new Money(fromActivity.UnitPrice.Currency, expectedPrice, date));
					}
				}
			}
		}

		private async Task AddOrUpdateSymbol(SymbolConfiguration symbolConfiguration, ManualSymbolConfiguration? manualSymbolConfiguration)
		{
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