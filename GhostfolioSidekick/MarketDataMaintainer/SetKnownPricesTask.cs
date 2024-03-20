using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class SetKnownPricesTask : IScheduledWork
	{
		private readonly ILogger<SetKnownPricesTask> logger;
		private readonly IMarketDataService marketDataService;
		private readonly IActivitiesService activitiesService;
		private readonly IApplicationSettings applicationSettings;
		private readonly IEnumerable<IHistoryDataFileImporter> historyDataFileImporters;

		public TaskPriority Priority => TaskPriority.SetKnownPrices;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public SetKnownPricesTask(
			ILogger<SetKnownPricesTask> logger,
			IMarketDataService marketDataManager,
			IActivitiesService activitiesManager,
			IApplicationSettings applicationSettings,
			IEnumerable<IHistoryDataFileImporter> historyDataFileImporters)
		{
			this.logger = logger;
			marketDataService = marketDataManager;
			activitiesService = activitiesManager;
			this.applicationSettings = applicationSettings;
			this.historyDataFileImporters = historyDataFileImporters;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(CreateManualSymbolTask)} Starting to do work");

			try
			{
				var profiles = (await marketDataService.GetAllSymbolProfiles()).ToList();
				var holdings = (await activitiesService.GetAllActivities()).ToList();

				var symbolConfigurations = applicationSettings.ConfigurationInstance.Symbols;

				List<SymbolProfile> symbolsAlreadyImported = [];
				if (!string.IsNullOrWhiteSpace(applicationSettings.ConfigurationInstance.Settings.HistoricDataFilePath))
				{
					symbolsAlreadyImported = await ImportFiles(profiles);
				}

				foreach (var symbolConfiguration in symbolConfigurations ?? [])
				{
					var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
					if (manualSymbolConfiguration == null || symbolsAlreadyImported.Select(x => x.Symbol).Contains(symbolConfiguration.Symbol))
					{
						continue;
					}

					await SetKnownPrices(symbolConfiguration, profiles, holdings);
				}
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogInformation($"{nameof(CreateManualSymbolTask)} Done");
		}

		private async Task SetKnownPrices(SymbolConfiguration symbolConfiguration, List<SymbolProfile> profiles, List<Holding> holdings)
		{
			var mdi = profiles.SingleOrDefault(x =>
				x.Symbol == symbolConfiguration.Symbol &&
				x.DataSource == Datasource.MANUAL &&
				x.AssetClass == Utilities.ParseAssetClass(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
				x.AssetSubClass == Utilities.ParseAssetSubClass(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass));
			if (mdi == null || mdi.ActivitiesCount <= 0)
			{
				return;
			}

			var activitiesForSymbol = holdings
				.Where(x =>
					x.SymbolProfile?.Symbol == mdi.Symbol &&
					x.SymbolProfile.DataSource == Datasource.MANUAL &&
					x.SymbolProfile.AssetClass == Utilities.ParseAssetClass(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
					x.SymbolProfile.AssetSubClass == Utilities.ParseAssetSubClass(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass))
				.SelectMany(x => x.Activities)
				.OfType<BuySellActivity>()
				.ToList();

			if (!activitiesForSymbol.Any())
			{
				return;
			}

			var md = (await marketDataService.GetMarketData(mdi.Symbol, mdi.DataSource.ToString())).MarketData;
			var sortedActivities = activitiesForSymbol
				.Where(x => x.UnitPrice?.Amount != 0)
				.GroupBy(x => x.Date.Date)
				.Select(x => x
					.OrderBy(x => x.TransactionId)
					.ThenByDescending(x => x.UnitPrice?.Amount ?? 0)
					.ThenByDescending(x => x.Quantity)
					.First())
				.OrderBy(x => x.Date)
				.ToList();

			for (var i = 0; i < sortedActivities.Count; i++)
			{
				var fromActivity = sortedActivities[i];
				if (fromActivity?.UnitPrice == null)
				{
					continue;
				}

				BuySellActivity? toActivity = null;

				if (i + 1 < sortedActivities.Count)
				{
					toActivity = sortedActivities[i + 1];
				}

				DateTime toDate = toActivity?.Date ?? DateTime.Today.AddDays(1);
				for (var date = fromActivity.Date; date <= toDate; date = date.AddDays(1))
				{
					var a = (decimal)(date - fromActivity.Date).TotalDays;
					var b = (decimal)(toDate - date).TotalDays;

					var percentage = a / (a + b);
					decimal amountFrom = fromActivity.UnitPrice!.Amount;
					decimal amountTo = toActivity?.UnitPrice?.Amount ?? fromActivity.UnitPrice?.Amount ?? 0;
					var expectedPrice = amountFrom + (percentage * (amountTo - amountFrom));

					var price = md.SingleOrDefault(x => x.Date.Date == date.Date);

					var diff = (price?.MarketPrice.Amount ?? 0) - expectedPrice;
					if (Math.Abs(diff) >= Constants.Epsilon)
					{
						var scraperDefined = symbolConfiguration?.ManualSymbolConfiguration?.ScraperConfiguration != null;
						var priceIsAvailable = (price?.MarketPrice.Amount ?? 0) != 0;
						var isToday = date >= DateTime.Today;
						var shouldSkip = scraperDefined && (priceIsAvailable || isToday);

						if (shouldSkip)
						{
							continue;
						}

						await marketDataService.SetMarketPrice(mdi, new Money(fromActivity.UnitPrice!.Currency, expectedPrice), date);
					}
				}
			}
		}

		private async Task<List<SymbolProfile>> ImportFiles(List<SymbolProfile> profiles)
		{
			var usedSymbols = new List<SymbolProfile>();
			var filePath = applicationSettings.ConfigurationInstance.Settings.HistoricDataFilePath!;
			foreach (var file in Directory.GetFiles(filePath))
			{
				var importer = historyDataFileImporters.SingleOrDefault(x => x.CanParseHistoricData(file).Result);
				if (importer == null)
				{
					continue;
				}

				var results = await importer.ParseHistoricData(file);
				foreach (var dataPoint in results.ToList())
				{
					var symbol = usedSymbols.SingleOrDefault(x => x.Symbol == dataPoint.Symbol);
					if (symbol == null)
					{
						symbol = profiles.Single(x => x.Symbol == dataPoint.Symbol);
						usedSymbols.Add(symbol);
					}

					await marketDataService.SetMarketPrice(symbol, new Money(symbol.Currency, dataPoint.Close), dataPoint.Date);
				}
			}

			return usedSymbols;
		}
	}
}