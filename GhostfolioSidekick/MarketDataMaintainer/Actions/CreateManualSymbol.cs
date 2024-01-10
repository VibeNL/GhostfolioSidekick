using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.MarketDataMaintainer.Actions
{
	internal class CreateManualSymbol
	{
		private readonly IGhostfolioAPI api;
		private readonly ConfigurationInstance configurationInstance;

		public CreateManualSymbol(
			IGhostfolioAPI api,
			ConfigurationInstance configurationInstance)
		{
			this.api = api;
			this.configurationInstance = configurationInstance;
		}

		internal async Task ManageManualSymbols()
		{
			var marketData = (await api.GetMarketData()).ToList();
			var activities = (await api.GetAllActivities()).ToList();

			var symbolConfigurations = configurationInstance.Symbols;
			foreach (var symbolConfiguration in symbolConfigurations ?? [])
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

		private async Task SetKnownPrices(SymbolConfiguration symbolConfiguration, List<MarketDataList> marketData, List<Activity> activities)
		{
			var mdi = marketData.SingleOrDefault(x => x.AssetProfile.Symbol == symbolConfiguration.Symbol);
			if (mdi == null || mdi.AssetProfile.ActivitiesCount <= 0 || mdi.AssetProfile.DataSource == null)
			{
				return;
			}

			var md = await api.GetMarketData(mdi.AssetProfile.Symbol, mdi.AssetProfile.DataSource);
			var activitiesForSymbol = activities.Where(x => x.Asset?.Symbol == mdi.AssetProfile.Symbol && IsBuyOrSell(x.ActivityType)).ToList();

			if (!activitiesForSymbol.Any())
			{
				return;
			}

			var sortedActivities = activitiesForSymbol
				.Where(x => x.UnitPrice?.Amount != 0)
				.GroupBy(x => x.Date)
				.Select(x => x
					.OrderBy(x => x.ReferenceCode)
					.ThenByDescending(x => x.UnitPrice)
					.ThenByDescending(x => x.Quantity)
					.ThenByDescending(x => x.ActivityType).First())
				.OrderBy(x => x.Date)
				.ToList();

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
					toActivity = new Activity
					(ActivityType.Undefined, fromActivity.Asset, DateTime.Today.AddDays(1), 0, fromActivity.UnitPrice, [], string.Empty, string.Empty);
				}

				for (var date = fromActivity.Date; date <= toActivity.Date; date = date.AddDays(1))
				{
					var a = (decimal)(date - fromActivity.Date).TotalDays;
					var b = (decimal)(toActivity.Date - date).TotalDays;

					var percentage = a / (a + b);
					decimal amountFrom = fromActivity.UnitPrice.Amount;
					decimal amountTo = toActivity.UnitPrice.Amount;
					var expectedPrice = amountFrom + (percentage * (amountTo - amountFrom));

					var price = md.MarketData.SingleOrDefault(x => x.Date.Date == date.Date);

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

						await api.SetMarketPrice(md.AssetProfile, new Money(fromActivity.UnitPrice.Currency, expectedPrice, date));
					}
				}
			}
		}

		private async Task AddOrUpdateSymbol(SymbolConfiguration symbolConfiguration, ManualSymbolConfiguration manualSymbolConfiguration)
		{
			AssetSubClass[]? expectedAssetSubClass = manualSymbolConfiguration.AssetSubClass != null ?
				[Utilities.ParseEnum<AssetSubClass>(manualSymbolConfiguration.AssetSubClass)] : null;
			var symbol = await api.FindSymbolByIdentifier(
				symbolConfiguration.Symbol,
				null,
				[Utilities.ParseEnum<AssetClass>(manualSymbolConfiguration.AssetClass)],
				expectedAssetSubClass,
				false);
			if (symbol == null)
			{
				await api.CreateManualSymbol(new SymbolProfile(
					CurrencyHelper.ParseCurrency(manualSymbolConfiguration.Currency),
					symbolConfiguration.Symbol,
					manualSymbolConfiguration.ISIN,
					manualSymbolConfiguration.Name,
					"MANUAL",
					Utilities.ParseEnum<AssetClass>(manualSymbolConfiguration.AssetClass),
					Utilities.ParseOptionalEnum<AssetSubClass>(manualSymbolConfiguration.AssetSubClass)
					));
			}

			symbol = await api.FindSymbolByIdentifier(
				symbolConfiguration.Symbol,
				null,
				[Utilities.ParseEnum<AssetClass>(manualSymbolConfiguration.AssetClass)],
				expectedAssetSubClass,
				false);
			if (symbol == null)
			{
				throw new NotSupportedException($"Symbol creation failed for symbol {symbolConfiguration.Symbol}");
			}

			// TODO: update symbol on difference???

			// Set scraper
			if (symbol.ScraperConfiguration.Url != manualSymbolConfiguration?.ScraperConfiguration?.Url ||
				symbol.ScraperConfiguration.Selector != manualSymbolConfiguration?.ScraperConfiguration?.Selector)
			{
				symbol.ScraperConfiguration.Url = manualSymbolConfiguration?.ScraperConfiguration?.Url;
				symbol.ScraperConfiguration.Selector = manualSymbolConfiguration?.ScraperConfiguration?.Selector;
				await api.UpdateSymbol(symbol);
			}
		}

		private static bool IsBuyOrSell(ActivityType activityType)
		{
			return activityType == ActivityType.Buy || activityType == ActivityType.Sell;
		}
	}
}
