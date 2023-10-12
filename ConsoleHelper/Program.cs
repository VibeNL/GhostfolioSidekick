using GhostfolioSidekick;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.FileImporter.Bunq;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.FileImporter.Generic;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.FileImporter.Trading212;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.MarketDataMaintainer;
using Microsoft.Extensions.Caching.Memory;

namespace ConsoleHelper
{
	internal class Program
	{
		private static ConsoleLogger logger = new ConsoleLogger();

		static void Main(string[] args)
		{
			Console.WriteLine("Hello, World!");

			foreach (var item in args)
			{
				var split = item.Split('=');
				Environment.SetEnvironmentVariable(split[0], split[1]);
			}

			var cs = new ConfigurationSettings();
			MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions { });
			GhostfolioAPI api = new GhostfolioAPI(cs, memoryCache, logger);
			IScheduledWork t = new FileImporterTask(logger, api, cs, new IFileImporter[] {
				new BunqParser(api),
				//new CoinbaseParser(api),
				new DeGiroParser(api),
				new GenericParser(api),
				new NexoParser(api),
				new ScalableCapitalParser(api),
				new Trading212Parser(api),
			});
			//t.DoWork().Wait();
			t = new MarketDataMaintainerTask(logger, api);
			t.DoWork().Wait();
		}
	}
}