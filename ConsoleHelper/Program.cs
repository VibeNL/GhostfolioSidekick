using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
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

            GhostfolioAPI api = new GhostfolioAPI(new MemoryCache(new MemoryCacheOptions{}), logger);
            var t = new FileImporterTask(logger, api, new IFileImporter[] { 
                new BaaderBankWUM(api),
                new BaaderBankRKK(api),
                new DeGiro(api)
            });
			t.DoWork().Wait();
        }
    }
}