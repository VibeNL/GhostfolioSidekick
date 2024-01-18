using GhostfolioSidekick;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Parsers;
using GhostfolioSidekick.Parsers.Bunq;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Parsers.Generic;
using GhostfolioSidekick.Parsers.NIBC;
using GhostfolioSidekick.Parsers.ScalableCaptial;
using GhostfolioSidekick.Parsers.Trading212;
using Microsoft.Extensions.Caching.Memory;

namespace ConsoleHelper
{
	internal static class Program
	{
		private static ConsoleLogger logger = new();

		static void Main(string[] args)
		{
			Console.WriteLine("Hello, World!");

			foreach (var item in args)
			{
				var split = item.Split('=');
				Environment.SetEnvironmentVariable(split[0], split[1]);
			}

			var cs = new ApplicationSettings();
			MemoryCache memoryCache = new(new MemoryCacheOptions { });
			//GhostfolioAPI api = new(cs, memoryCache, logger);
			var tasks = new IScheduledWork[]{
			new DisplayInformationTask(logger, cs),
			//new AccountMaintainerTask(logger, api, cs),
			//new CreateManualSymbolTask(logger, api, cs),
			new FileImporterTask(logger, cs, new IFileImporter[] {
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