using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class SetManualPricesTask : IScheduledWork
	{
		private readonly ILogger<CreateManualSymbolTask> logger;
		private readonly IMarketDataService marketDataService;
		private readonly IActivitiesService activitiesService;
		private readonly IEnumerable<IFileImporter> importers;
		private readonly IApplicationSettings applicationSettings;
		private readonly string fileLocation;

		public TaskPriority Priority => TaskPriority.SetManualPrices;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public SetManualPricesTask(
			ILogger<CreateManualSymbolTask> logger,
			IMarketDataService marketDataManager,
			IActivitiesService activitiesManager,
			IEnumerable<IFileImporter> importers,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.marketDataService = marketDataManager;
			this.activitiesService = activitiesManager;
			this.importers = importers;
			this.applicationSettings = applicationSettings;
			fileLocation = applicationSettings.FileImporterPath;
		}

		public async Task DoWork()
		{
			logger.LogDebug($"{nameof(SetManualPricesTask)} Starting to do work");

			try
			{
				var profiles = (await marketDataService.GetAllSymbolProfiles()).ToList();
				var holdings = (await activitiesService.GetAllActivities()).ToList();
				var directory = new DirectoryInfo(fileLocation);

				var historicData = new List<HistoricData>();

				try
				{
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName)
						.Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase));

					foreach (var file in files)
					{
						var importer = importers.SingleOrDefault(x => x.CanParse(file).Result) ?? throw new NoImporterAvailableException($"File {file} has no importer");

						if (importer is IHistoryDataFileImporter activityImporter)
						{
							historicData.AddRange(await activityImporter.ParseHistoricData(file));
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error {Message}", ex.Message);
				}

				var symbolConfigurations = applicationSettings.ConfigurationInstance.Symbols;
				foreach (var symbolConfiguration in symbolConfigurations ?? [])
				{
					var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
					if (manualSymbolConfiguration == null)
					{
						continue;
					}

					await SetKnownPrices(symbolConfiguration, profiles, holdings, historicData);
				}
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogDebug($"{nameof(SetManualPricesTask)} Done");
		}

		private async Task SetKnownPrices(SymbolConfiguration symbolConfiguration, List<SymbolProfile> profiles, List<Holding> holdings, List<HistoricData> historicData)
		{
			var mdi = GetMatchingProfile(symbolConfiguration, profiles);
			if (mdi == null || mdi.ActivitiesCount <= 0)
			{
				return;
			}

			var activitiesForSymbol = GetActivitiesForSymbol(symbolConfiguration, mdi, holdings);
			if (!activitiesForSymbol.Any())
			{
				return;
			}

			var md = (await marketDataService.GetMarketData(mdi.Symbol, mdi.DataSource.ToString())).MarketData;
			var sortedActivities = SortActivities(activitiesForSymbol);

			await ProcessActivities(symbolConfiguration, mdi, md, sortedActivities, historicData);
		}

		private SymbolProfile? GetMatchingProfile(SymbolConfiguration symbolConfiguration, List<SymbolProfile> profiles)
		{
			return profiles.SingleOrDefault(x =>
				x.Symbol == symbolConfiguration.Symbol &&
				x.DataSource == Datasource.MANUAL &&
				x.AssetClass == EnumMapper.ParseAssetClass(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
				x.AssetSubClass == EnumMapper.ParseAssetSubClass(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass));
		}

		private List<BuySellActivity> GetActivitiesForSymbol(SymbolConfiguration symbolConfiguration, SymbolProfile mdi, List<Holding> holdings)
		{
			return holdings
				.Where(x =>
					x.SymbolProfile?.Symbol == mdi.Symbol &&
					x.SymbolProfile.DataSource == Datasource.MANUAL &&
					x.SymbolProfile.AssetClass == EnumMapper.ParseAssetClass(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
					x.SymbolProfile.AssetSubClass == EnumMapper.ParseAssetSubClass(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass))
				.SelectMany(x => x.Activities)
				.OfType<BuySellActivity>()
				.ToList();
		}

		private List<BuySellActivity> SortActivities(List<BuySellActivity> activitiesForSymbol)
		{
			return activitiesForSymbol
				.Where(x => x.UnitPrice?.Amount != 0)
				.GroupBy(x => x.Date.Date)
				.Select(x => x
					.OrderBy(x => x.TransactionId)
					.ThenByDescending(x => x.UnitPrice?.Amount ?? 0)
					.ThenByDescending(x => x.Quantity)
					.First())
				.OrderBy(x => x.Date)
				.ToList();
		}

		private async Task ProcessActivities(SymbolConfiguration symbolConfiguration, SymbolProfile mdi, List<MarketData> md, List<BuySellActivity> sortedActivities, List<HistoricData> historicData)
		{
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
					decimal expectedPrice = CalculateExpectedPrice(symbolConfiguration, fromActivity, toActivity, date, historicData);
					var priceFromGhostfolio = md.SingleOrDefault(x => x.Date.Date == date.Date);

					var diff = (priceFromGhostfolio?.MarketPrice.Amount ?? 0) - expectedPrice;
					if (Math.Abs(diff) >= Constants.Epsilon)
					{
						var shouldSkip = ShouldSkipPriceUpdate(symbolConfiguration, priceFromGhostfolio, date);
						if (shouldSkip)
						{
							continue;
						}

						await marketDataService.SetMarketPrice(mdi, new Money(fromActivity.UnitPrice!.Currency, expectedPrice), date);
					}
				}
			}
		}

		private decimal CalculateExpectedPrice(SymbolConfiguration symbolConfiguration, BuySellActivity fromActivity, BuySellActivity? toActivity, DateTime date, List<HistoricData> historicData)
		{
			var knownPrice = historicData.SingleOrDefault(x => x.Symbol == symbolConfiguration!.Symbol && x.Date.Date == date.Date);
			if (knownPrice != null)
			{
				return knownPrice.Close;
			}
			else
			{
				var a = (decimal)(date - fromActivity.Date).TotalDays;
				var b = (decimal)((toActivity?.Date ?? DateTime.Today.AddDays(1)) - date).TotalDays;

				var percentage = a / (a + b);
				decimal amountFrom = fromActivity.UnitPrice!.Amount;
				decimal amountTo = toActivity?.UnitPrice?.Amount ?? fromActivity.UnitPrice?.Amount ?? 0;
				return amountFrom + (percentage * (amountTo - amountFrom));
			}
		}

		private bool ShouldSkipPriceUpdate(SymbolConfiguration symbolConfiguration, MarketData? priceFromGhostfolio, DateTime date)
		{
			var scraperDefined = symbolConfiguration?.ManualSymbolConfiguration?.ScraperConfiguration != null;
			var priceIsAvailable = (priceFromGhostfolio?.MarketPrice.Amount ?? 0) != 0;
			var isToday = date >= DateTime.Today;
			return scraperDefined && (priceIsAvailable || isToday);
		}
	}
}