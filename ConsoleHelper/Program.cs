using GhostfolioSidekick;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.FileImporter.Bunq;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.FileImporter.Generic;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.FileImporter.NIBC;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.FileImporter.Trading212;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.MarketDataMaintainer;
using Microsoft.Extensions.Caching.Memory;

namespace ConsoleHelper
{
	internal class Program
	{
		private static ConsoleLogger logger = new();

		static void Main(string[] args)
		{
			foreach (var item in args)
			{
				var split = item.Split('=');
				Environment.SetEnvironmentVariable(split[0], split[1]);
			}

			var cs = new ApplicationSettings();
			MemoryCache memoryCache = new(new MemoryCacheOptions { });
			GhostfolioAPI api = new(cs, memoryCache, logger);
			var tasks = new IScheduledWork[]{
			new DisplayInformationTask(logger, cs),
			new AccountMaintainerTask(logger, api, cs),
			new CreateManualSymbolTask(logger, api, cs),
			new FileImporterTask(logger, api, cs, new IFileImporter[] {
				new BitvavoParser(cs, api),
				new BunqParser(api),
				new DeGiroParser(api),
				new GenericParser(api),
				new NexoParser(cs, api),
				new NIBCParser(api),
				new ScalableCapitalParser(api),
				new Trading212Parser(api)
			}),
			new MarketDataMaintainerTask(logger, api, cs)
			};

			foreach (var t in tasks.OrderBy(x => x.Priority))
			{
				t.DoWork().Wait();
			}
		}
	}
}