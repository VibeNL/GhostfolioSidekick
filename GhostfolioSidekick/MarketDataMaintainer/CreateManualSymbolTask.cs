using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class CreateManualSymbolTask : IScheduledWork
	{
		private readonly ILogger<FileImporterTask> logger;
		private readonly IAccountService accountService;
		private readonly IMarketDataService marketDataService;
		private readonly IActivitiesService activitiesService;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.CreateManualSymbols;

		public CreateManualSymbolTask(
			ILogger<FileImporterTask> logger,
			IAccountService accountManager,
			IMarketDataService marketDataManager,
			IActivitiesService activitiesManager,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.accountService = accountManager;
			this.marketDataService = marketDataManager;
			this.activitiesService = activitiesManager;
			this.applicationSettings = applicationSettings;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(CreateManualSymbolTask)} Starting to do work");

			try
			{
				var profiles = (await marketDataService.GetAllSymbolProfiles()).ToList();
				var holdings = (await activitiesService.GetAllActivities()).ToList();

				var symbolConfigurations = applicationSettings.ConfigurationInstance.Symbols;
				foreach (var symbolConfiguration in symbolConfigurations ?? [])
				{
					var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
					if (manualSymbolConfiguration == null)
					{
						continue;
					}

					await AddOrUpdateSymbol(symbolConfiguration, manualSymbolConfiguration);
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
				x.AssetClass == Utilities.ParseEnum<AssetClass>(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
				x.AssetSubClass == Utilities.ParseOptionalEnum<AssetSubClass>(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass));
			if (mdi == null || mdi.ActivitiesCount <= 0)
			{
				return;
			}

			var activitiesForSymbol = holdings
				.Where(x =>
					x.SymbolProfile?.Symbol == mdi.Symbol &&
					x.SymbolProfile.DataSource == Datasource.MANUAL &&
					x.SymbolProfile.AssetClass == Utilities.ParseEnum<AssetClass>(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
					x.SymbolProfile.AssetSubClass == Utilities.ParseOptionalEnum<AssetSubClass>(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass))
				.SelectMany(x => x.Activities)
				.Where(x => IsBuyOrSell(x.ActivityType)).ToList();

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
					.ThenByDescending(x => x.UnitPrice.Amount)
					.ThenByDescending(x => x.Quantity)
					.ThenByDescending(x => x.ActivityType).First())
				.OrderBy(x => x.Date)
				.ToList();

			for (var i = 0; i < sortedActivities.Count; i++)
			{
				var fromActivity = sortedActivities[i];
				Activity? toActivity = null;

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
					decimal amountFrom = fromActivity.UnitPrice.Amount;
					decimal amountTo = toActivity?.UnitPrice.Amount ?? fromActivity.UnitPrice.Amount;
					var expectedPrice = amountFrom + (percentage * (amountTo - amountFrom));

					var price = md.SingleOrDefault(x => x.Date.Date == date.Date);

					var diff = (price?.MarketPrice ?? 0) - expectedPrice;
					if (Math.Abs(diff) >= Constants.Epsilon)
					{
						var scraperDefined = symbolConfiguration?.ManualSymbolConfiguration?.ScraperConfiguration != null;
						var priceIsAvailable = price?.MarketPrice != null;
						var isToday = date >= DateTime.Today;
						var shouldSkip = scraperDefined && (priceIsAvailable || isToday);

						if (shouldSkip)
						{
							continue;
						}

						await marketDataService.SetMarketPrice(mdi, new Money(fromActivity.UnitPrice.Currency, expectedPrice), date);
					}
				}
			}
		}

		private async Task AddOrUpdateSymbol(SymbolConfiguration symbolConfiguration, ManualSymbolConfiguration manualSymbolConfiguration)
		{
			var subClass = Utilities.ParseOptionalEnum<AssetSubClass>(manualSymbolConfiguration.AssetSubClass);
			AssetSubClass[]? expectedAssetSubClass = subClass != null ? [subClass.Value] : null;
			var symbol = await marketDataService.FindSymbolByIdentifier(
				[symbolConfiguration.Symbol],
				null,
				[Utilities.ParseEnum<AssetClass>(manualSymbolConfiguration.AssetClass)],
				expectedAssetSubClass,
				false,
				false);
			if (symbol == null)
			{
				await marketDataService.CreateSymbol(new SymbolProfile(
					symbolConfiguration.Symbol,
					manualSymbolConfiguration.Name,
					new Currency(manualSymbolConfiguration.Currency),
					Datasource.MANUAL,
					Utilities.ParseEnum<AssetClass>(manualSymbolConfiguration.AssetClass),
					Utilities.ParseOptionalEnum<AssetSubClass>(manualSymbolConfiguration.AssetSubClass))
				{
					ISIN = manualSymbolConfiguration.ISIN
				}
				);
			}

			symbol = await marketDataService.FindSymbolByIdentifier(
				[symbolConfiguration.Symbol],
				null,
				[Utilities.ParseEnum<AssetClass>(manualSymbolConfiguration.AssetClass)],
				expectedAssetSubClass,
				false,
				false);
			if (symbol == null)
			{
				throw new NotSupportedException($"Symbol creation failed for symbol {symbolConfiguration.Symbol}");
			}

			// TODO: update symbol on difference???

			// Set scraper
			if (symbol.ScraperConfiguration.Url != manualSymbolConfiguration?.ScraperConfiguration?.Url ||
				symbol.ScraperConfiguration.Selector != manualSymbolConfiguration?.ScraperConfiguration?.Selector ||
				symbol.ScraperConfiguration.Locale != manualSymbolConfiguration?.ScraperConfiguration?.Locale
				)
			{
				symbol.ScraperConfiguration.Url = manualSymbolConfiguration?.ScraperConfiguration?.Url;
				symbol.ScraperConfiguration.Selector = manualSymbolConfiguration?.ScraperConfiguration?.Selector;
				symbol.ScraperConfiguration.Locale = manualSymbolConfiguration?.ScraperConfiguration?.Locale;
				await marketDataService.UpdateSymbol(symbol);
			}
		}

		private static bool IsBuyOrSell(ActivityType activityType)
		{
			return activityType == ActivityType.Buy || activityType == ActivityType.Sell;
		}
	}
}