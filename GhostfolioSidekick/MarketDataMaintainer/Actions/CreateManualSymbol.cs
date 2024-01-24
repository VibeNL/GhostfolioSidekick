using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.MarketDataMaintainer.Actions
{
	internal class CreateManualSymbol
	{
		private readonly IAccountService accountService;
		private readonly IMarketDataService marketDataService;
		private readonly IActivitiesService activitiesService;
		private readonly ConfigurationInstance configurationInstance;

		public CreateManualSymbol(
			IAccountService accountManager,
			IMarketDataService marketDataManager,
			IActivitiesService activitiesManager,
			ConfigurationInstance configurationInstance)
		{
			accountService = accountManager;
			marketDataService = marketDataManager;
			activitiesService = activitiesManager;
			this.configurationInstance = configurationInstance;
		}

		internal async Task ManageManualSymbols()
		{
			var marketData = (await marketDataService.GetMarketData()).ToList();
			var holdings = (await activitiesService.GetAllActivities()).ToList();

			var symbolConfigurations = configurationInstance.Symbols;
			foreach (var symbolConfiguration in symbolConfigurations ?? [])
			{
				var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
				if (manualSymbolConfiguration == null)
				{
					continue;
				}

				await AddOrUpdateSymbol(symbolConfiguration, manualSymbolConfiguration);
				await SetKnownPrices(symbolConfiguration, marketData, holdings);
			}
		}

		private async Task SetKnownPrices(SymbolConfiguration symbolConfiguration, List<MarketDataProfile> marketData, List<Holding> holdings)
		{
			var mdi = marketData.SingleOrDefault(x =>
				x.AssetProfile.Symbol == symbolConfiguration.Symbol &&
				x.AssetProfile.DataSource == Datasource.MANUAL &&
				x.AssetProfile.AssetClass == Utilities.ParseEnum<AssetClass>(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
				x.AssetProfile.AssetSubClass == Utilities.ParseOptionalEnum<AssetSubClass>(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass));
			if (mdi == null || mdi.AssetProfile.ActivitiesCount <= 0)
			{
				return;
			}

			var md = mdi.MarketData;
			var activitiesForSymbol = holdings
				.Where(x =>
					x.SymbolProfile?.Symbol == mdi.AssetProfile.Symbol &&
					x.SymbolProfile.DataSource == Datasource.MANUAL &&
					x.SymbolProfile.AssetClass == Utilities.ParseEnum<AssetClass>(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
					x.SymbolProfile.AssetSubClass == Utilities.ParseOptionalEnum<AssetSubClass>(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass))
				.SelectMany(x => x.Activities)
				.Where(x => IsBuyOrSell(x.ActivityType)).ToList();

			if (!activitiesForSymbol.Any())
			{
				return;
			}

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
					if (Math.Abs(diff) >= 0.00001M)
					{
						var scraperDefined = symbolConfiguration?.ManualSymbolConfiguration?.ScraperConfiguration != null;
						var priceIsAvailable = price?.MarketPrice != null;
						var isToday = date >= DateTime.Today;
						var shouldSkip = scraperDefined && (priceIsAvailable || isToday);

						if (shouldSkip)
						{
							continue;
						}

						await marketDataService.SetMarketPrice(mdi.AssetProfile, new Money(fromActivity.UnitPrice.Currency, expectedPrice));
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
