using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Parsers;
using GhostfolioSidekick.Parsers.Bunq;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Parsers.Generic;
using GhostfolioSidekick.Parsers.NIBC;
using GhostfolioSidekick.Parsers.ScalableCaptial;
using GhostfolioSidekick.Parsers.Trading212;
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
			IExchangeRateService exchangeRateService = { };
			IMarketDataService marketDataManager = new MarketDataService(
				settings,
				memoryCache,
				new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken),
				logger);
			IAccountService accountManager = new AccountService(
				new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken));
			IActivitiesService activitiesManager = new ActivitiesService(
				exchangeRateService,
				accountManager,
				new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken),
				logger);
			var tasks = new IScheduledWork[]{
			new DisplayInformationTask(logger, settings),
			//new AccountMaintainerTask(logger, api, cs),
			//new CreateManualSymbolTask(logger, api, cs),
			new FileImporterTask(logger, settings, activitiesManager, accountManager, marketDataManager, new IFileImporter[] {
				//new BitvavoParser(cs, api),
				new BunqParser(),
				//new CoinbaseParser(cs, api),
				new DeGiroParserNL(),
				new DeGiroParserPT(),
				new GenericParser(),
				//new NexoParser(cs, api),
				new NIBCParser(),
				new ScalableCapitalRKKParser(),
				new ScalableCapitalWUMParser(),
				new Trading212Parser()
			}),
			//new MarketDataMaintainerTask(logger, api, cs)
			};

			foreach (var t in tasks.OrderBy(x => x.Priority))
			{
				t.DoWork().Wait();
			}
		}
	}
}