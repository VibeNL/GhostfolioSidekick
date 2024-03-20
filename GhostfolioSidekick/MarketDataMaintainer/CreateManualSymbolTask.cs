using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class CreateManualSymbolTask : IScheduledWork
	{
		private readonly ILogger<CreateManualSymbolTask> logger;
		private readonly IMarketDataService marketDataService;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.CreateManualSymbols;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public CreateManualSymbolTask(
			ILogger<CreateManualSymbolTask> logger,
			IMarketDataService marketDataManager,
			IApplicationSettings applicationSettings)
		{
			this.logger = logger;
			marketDataService = marketDataManager;
			this.applicationSettings = applicationSettings;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(CreateManualSymbolTask)} Starting to do work");

			try
			{
				var symbolConfigurations = applicationSettings.ConfigurationInstance.Symbols;
				foreach (var symbolConfiguration in symbolConfigurations ?? [])
				{
					var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
					if (manualSymbolConfiguration == null)
					{
						continue;
					}

					await AddAndUpdateSymbol(symbolConfiguration, manualSymbolConfiguration);
				}
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogInformation($"{nameof(CreateManualSymbolTask)} Done");
		}

		private async Task AddAndUpdateSymbol(SymbolConfiguration symbolConfiguration, ManualSymbolConfiguration manualSymbolConfiguration)
		{
			var subClass = Utilities.ParseAssetSubClass(manualSymbolConfiguration.AssetSubClass);
			AssetSubClass[]? expectedAssetSubClass = subClass != null ? [subClass.Value] : null;
			var symbol = await marketDataService.FindSymbolByIdentifier(
				[symbolConfiguration.Symbol],
				null,
				[Utilities.ParseAssetClass(manualSymbolConfiguration.AssetClass)],
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
					Utilities.ParseAssetClass(manualSymbolConfiguration.AssetClass),
					Utilities.ParseAssetSubClass(manualSymbolConfiguration.AssetSubClass),
					manualSymbolConfiguration.Countries.Select(x => new Model.Symbols.Country(x.Name, x.Code, x.Continent, x.Weight)).ToArray(),
					manualSymbolConfiguration.Sectors.Select(x => new Model.Symbols.Sector(x.Name, x.Weight)).ToArray())
				{
					ISIN = manualSymbolConfiguration.ISIN
				}
				);
			}

			symbol = await marketDataService.FindSymbolByIdentifier(
				[symbolConfiguration.Symbol],
				null,
				[Utilities.ParseAssetClass(manualSymbolConfiguration.AssetClass)],
				expectedAssetSubClass,
				false,
				false);
			if (symbol == null)
			{
				throw new NotSupportedException($"Symbol creation failed for symbol {symbolConfiguration.Symbol}");
			}

			// Set scraper
			if (symbol.ScraperConfiguration.Url != manualSymbolConfiguration.ScraperConfiguration?.Url ||
				symbol.ScraperConfiguration.Selector != manualSymbolConfiguration.ScraperConfiguration?.Selector ||
				symbol.ScraperConfiguration.Locale != manualSymbolConfiguration.ScraperConfiguration?.Locale
				)
			{
				symbol.ScraperConfiguration.Url = manualSymbolConfiguration.ScraperConfiguration?.Url;
				symbol.ScraperConfiguration.Selector = manualSymbolConfiguration.ScraperConfiguration?.Selector;
				symbol.ScraperConfiguration.Locale = manualSymbolConfiguration.ScraperConfiguration?.Locale;
				await marketDataService.UpdateSymbol(symbol);
			}

			// Set countries, TODO: check all properties
			var countries = manualSymbolConfiguration.Countries;
			if (countries != null && !countries.Select(x => x.Code).SequenceEqual(symbol.Countries.Select(x => x.Code)))
			{
				symbol.Countries = countries.Select(x => new Model.Symbols.Country(x.Name, x.Code, x.Continent, x.Weight));
				await marketDataService.UpdateSymbol(symbol);
			}

			// Set sectors, TODO: check all properties
			var sectors = manualSymbolConfiguration.Sectors;
			if (sectors != null && !sectors.Select(x => x.Name).SequenceEqual(symbol.Sectors.Select(x => x.Name)))
			{
				symbol.Sectors = sectors.Select(x => new Model.Symbols.Sector(x.Name, x.Weight));
				await marketDataService.UpdateSymbol(symbol);
			}
		}
	}
}