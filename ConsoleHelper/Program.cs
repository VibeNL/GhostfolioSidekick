﻿using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Cryptocurrency.Workarouunds;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Parsers;
using GhostfolioSidekick.Parsers.Bitvavo;
using GhostfolioSidekick.Parsers.Bunq;
using GhostfolioSidekick.Parsers.Coinbase;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Parsers.Generic;
using GhostfolioSidekick.Parsers.Nexo;
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
			RestCall restCall = new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken);
			IExchangeRateService exchangeRateService = new ExchangeRateService(
				restCall,
				logger);
			IMarketDataService marketDataManager = new MarketDataService(
				settings,
				memoryCache,
				restCall,
				logger);
			IAccountService accountManager = new AccountService(
				settings,
				restCall,
				logger);
			IActivitiesService activitiesManager = new ActivitiesService(
				exchangeRateService,
				accountManager,
				restCall,
				logger);
			var tasks = new IScheduledWork[]{
			new DisplayInformationTask(logger, settings),
			//new AccountMaintainerTask(logger, accountManager, settings),
			//new CreateManualSymbolTask(logger, accountManager, marketDataManager, activitiesManager, settings),
			new FileImporterTask(logger, settings, activitiesManager, accountManager, marketDataManager, new IFileImporter[] {
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
				new ApplyDust(settings.ConfigurationInstance.Settings),
				new StakeAsDividend(settings.ConfigurationInstance.Settings) }),
			//new MarketDataMaintainerTask(logger, api, cs)
			};

			foreach (var t in tasks.OrderBy(x => x.Priority))
			{
				t.DoWork().Wait();
			}
		}
	}
}