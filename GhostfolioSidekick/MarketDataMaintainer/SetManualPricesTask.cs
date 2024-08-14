////using GhostfolioSidekick.Configuration;
////using GhostfolioSidekick.FileImporter;
////using GhostfolioSidekick.GhostfolioAPI;
////using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
////using GhostfolioSidekick.Model.Activities;
////using GhostfolioSidekick.Model.Activities.Types;
////using GhostfolioSidekick.Model.Symbols;
////using GhostfolioSidekick.Parsers;
////using Microsoft.Extensions.Logging;

////namespace GhostfolioSidekick.MarketDataMaintainer
////{
////	public class SetManualPricesTask : IScheduledWork
////	{
////		private readonly ILogger<CreateManualSymbolTask> logger;
////		private readonly IMarketDataService marketDataService;
////		private readonly IActivitiesService activitiesService;
////		private readonly IEnumerable<IFileImporter> importers;
////		private readonly IApplicationSettings applicationSettings;
////		private readonly string fileLocation;
////		private readonly ManualPriceProcessor manualPricesProcessor;

////		public TaskPriority Priority => TaskPriority.SetManualPrices;

////		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

////		public SetManualPricesTask(
////			ILogger<CreateManualSymbolTask> logger,
////			IMarketDataService marketDataManager,
////			IActivitiesService activitiesManager,
////			IEnumerable<IFileImporter> importers,
////			IApplicationSettings applicationSettings)
////		{
////			ArgumentNullException.ThrowIfNull(applicationSettings);

////			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
////			this.marketDataService = marketDataManager;
////			this.activitiesService = activitiesManager;
////			this.importers = importers;
////			this.applicationSettings = applicationSettings;
////			fileLocation = applicationSettings.FileImporterPath;

////			manualPricesProcessor = new ManualPriceProcessor(marketDataManager);
////		}

////		public async Task DoWork()
////		{
////			logger.LogDebug($"{nameof(SetManualPricesTask)} Starting to do work");

////			try
////			{
////				var profiles = (await marketDataService.GetAllSymbolProfiles()).ToList();
////				var holdings = (await activitiesService.GetAllActivities()).ToList();
////				var directory = new DirectoryInfo(fileLocation);

////				var historicData = new List<HistoricData>();

////				try
////				{
////					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName)
////						.Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase));

////					foreach (var file in files)
////					{
////						var importer = importers.SingleOrDefault(x => x.CanParse(file).Result) ?? throw new NoImporterAvailableException($"File {file} has no importer");

////						if (importer is IHistoryDataFileImporter activityImporter)
////						{
////							historicData.AddRange(await activityImporter.ParseHistoricData(file));
////						}
////					}

////					await MapSymbolsToKnownSymbols(historicData);
////				}
////				catch (Exception ex)
////				{
////					logger.LogError(ex, "Error {Message}", ex.Message);
////				}

////				var symbolConfigurations = applicationSettings.ConfigurationInstance.Symbols;
////				foreach (var symbolConfiguration in symbolConfigurations ?? [])
////				{
////					var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
////					if (manualSymbolConfiguration == null)
////					{
////						continue;
////					}

////					await SetKnownPrices(symbolConfiguration, profiles, holdings, historicData);
////				}
////			}
////			catch (NotAuthorizedException)
////			{
////				// Running against a managed instance?
////				applicationSettings.AllowAdminCalls = false;
////			}

////			logger.LogDebug($"{nameof(SetManualPricesTask)} Done");
////		}

////		private async Task MapSymbolsToKnownSymbols(List<HistoricData> historicData)
////		{
////			foreach (var symbolFromHistoricData in historicData.Select(x => x.Symbol).Distinct())
////			{
////				var symbol = await marketDataService.FindSymbolByIdentifier(
////					[symbolFromHistoricData],
////					null,
////					null,
////					null,
////					true,
////					false);
////				if (symbol != null)
////				{
////					historicData.ForEach(x => x.Symbol = symbol.Symbol);
////				}
////			}
////		}

////		private async Task SetKnownPrices(SymbolConfiguration symbolConfiguration, List<SymbolProfile> profiles, List<Holding> holdings, List<HistoricData> historicData)
////		{
////			var mdi = GetMatchingProfile(symbolConfiguration, profiles);
////			if (mdi == null || mdi.ActivitiesCount <= 0)
////			{
////				return;
////			}

////			var activitiesForSymbol = GetActivitiesForSymbol(symbolConfiguration, mdi, holdings);
////			if (!activitiesForSymbol.Any())
////			{
////				return;
////			}

////			var md = (await marketDataService.GetMarketData(mdi.Symbol, mdi.DataSource.ToString())).MarketData;

////			await manualPricesProcessor.ProcessActivities(symbolConfiguration, mdi, md, activitiesForSymbol, historicData);
////		}

////		private SymbolProfile? GetMatchingProfile(SymbolConfiguration symbolConfiguration, List<SymbolProfile> profiles)
////		{
////			return profiles.SingleOrDefault(x =>
////				x.Symbol == symbolConfiguration.Symbol &&
////				x.DataSource == Datasource.MANUAL &&
////				x.AssetClass == EnumMapper.ParseAssetClass(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
////				x.AssetSubClass == EnumMapper.ParseAssetSubClass(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass));
////		}

////		private List<BuySellActivity> GetActivitiesForSymbol(SymbolConfiguration symbolConfiguration, SymbolProfile mdi, List<Holding> holdings)
////		{
////			return holdings
////				.Where(x =>
////					x.SymbolProfile?.Symbol == mdi.Symbol &&
////					x.SymbolProfile.DataSource == Datasource.MANUAL &&
////					x.SymbolProfile.AssetClass == EnumMapper.ParseAssetClass(symbolConfiguration.ManualSymbolConfiguration!.AssetClass) &&
////					x.SymbolProfile.AssetSubClass == EnumMapper.ParseAssetSubClass(symbolConfiguration.ManualSymbolConfiguration.AssetSubClass))
////				.SelectMany(x => x.Activities)
////				.OfType<BuySellActivity>()
////				.ToList();
////		}
////	}
////}