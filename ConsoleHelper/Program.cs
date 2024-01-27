using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.MarketDataMaintainer;
using Microsoft.Extensions.Caching.Memory;

namespace GhostfolioSidekick.ConsoleHelper
{
	internal static class Program
	{
		private static readonly ConsoleLogger logger = new();

		static void Main(string[] args)
		{
			Console.WriteLine("Hello, World!");

			foreach (var item in args)
			{
				var split = item.Split('=');
				Environment.SetEnvironmentVariable(split[0], split[1]);
			}

			var settings = new ApplicationSettings();
			MemoryCache memoryCache = new(new MemoryCacheOptions { });
			RestCall restCall = new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken);
			IExchangeRateService exchangeRateService = new ExchangeRateService(
				restCall,
				logger);
			IMarketDataService marketDataService = new MarketDataService(
				settings,
				memoryCache,
				restCall,
				logger);
			IAccountService accountService = new AccountService(
				settings,
				restCall,
				logger);
			IActivitiesService activitiesService = new ActivitiesService(
				exchangeRateService,
				accountService,
				restCall,
				logger);
			var tasks = new IScheduledWork[]{
			new DisplayInformationTask(logger, settings),
			//new AccountMaintainerTask(logger, accountService, settings),
			new CreateManualSymbolTask(logger, accountService, marketDataService, activitiesService, settings),
			new DeleteUnusedSymbolsTask(logger, marketDataService, settings),
			new SetBenchmarksTask(logger, marketDataService, settings),
			new SetTrackingInsightOnSymbolsTask(logger, marketDataService, settings),
			/*new FileImporterTask(logger, settings, activitiesService, accountService, marketDataService, new IFileImporter[] {
				new BitvavoParser(),
				new BunqParser(),
				new CoinbaseParser(),
				new DeGiroParserNL(),
				new DeGiroParserPT(),
				new GenericParser(),
				new NexoParser(),
				new NIBCParser(),
				new ScalableCapitalRKKParser(),
				new ScalableCapitalWUMParser(),
				new Trading212Parser()
			}, new IHoldingStrategy[] {
				new DeterminePrice(marketDataService),
				new ApplyDustCorrectionWorkaround(settings.ConfigurationInstance.Settings),
				new StakeAsDividendWorkaround(settings.ConfigurationInstance.Settings) }),*/
			};

			foreach (var t in tasks.OrderBy(x => x.Priority))
			{
				t.DoWork().Wait();
			}
		}
	}
}