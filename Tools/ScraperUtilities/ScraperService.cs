using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities
{
	public class ScraperService(ILogger<ScraperService> logger, IPlaywright playwright) : IHostedService
	{
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Get output dir
			Console.WriteLine("Enter the output directory path:");
			var outputDirectory = Console.ReadLine();
			if (outputDirectory == null)
			{
				return;
			}

			while (!cancellationToken.IsCancellationRequested)
			{
				Console.WriteLine("Select your scraper");
				Console.WriteLine("1. Scalable Capital");
				Console.WriteLine("2. Trade Republic");
				Console.WriteLine("3. Centraal Beheer");
				Console.WriteLine("0. Exit");
				var input = Console.ReadLine();
				if (input == null)
				{
					continue;
				}

				SupportedBrokers? broker;
				switch (input)
				{
					case "1":
						broker = SupportedBrokers.ScalableCapital;
						break;
					case "2":
						broker = SupportedBrokers.TradeRepublic;
						break;
					case "3":
						broker = SupportedBrokers.CentraalBeheer;
						break;
					case "0":
						Environment.Exit(0);
						return;
					default:
						Console.WriteLine("Invalid input.");
						continue;
				}

				await RunAsync(broker.Value, outputDirectory);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public async Task RunAsync(SupportedBrokers broker, string outputDirectory)
		{
			var browser = await playwright.Chromium.ConnectOverCDPAsync("http://localhost:9222");
			var defaultContext = browser.Contexts[0];

			try
			{
				var page = defaultContext.Pages[0];

				logger.LogInformation("Starting the scraping process...");
				logger.LogInformation("Broker: {Broker}", broker);
				logger.LogInformation("Output directory: {OutputDirectory}", outputDirectory);

				Dictionary<int, IEnumerable<ActivityWithSymbol>> transactions = [];
				switch (broker)
				{
					case SupportedBrokers.ScalableCapital:
						{
							var scraper = new ScalableCapital.Scraper(page, logger);
							transactions = await scraper.ScrapeTransactions();
						}
						break;
					case SupportedBrokers.TradeRepublic:
						{
							var scraper = new TradeRepublic.Scraper(page, logger, outputDirectory);
							transactions.Add(0, await scraper.ScrapeTransactions());
						}
						break;
					case SupportedBrokers.CentraalBeheer:
						{
							var scraper = new CentraalBeheer.Scraper(page, logger);
							transactions.Add(0, await scraper.ScrapeTransactions());
						}
						break;
					default:
						throw new ArgumentException("Invalid broker entered.");
				}

				CsvHelperService.SaveToCSV(outputDirectory, broker.ToString(), transactions);
				logger.LogInformation("Scraping process completed.");
			}
			finally
			{
				await defaultContext.CloseAsync();
				await browser.CloseAsync();
			}
		}
	}
}
