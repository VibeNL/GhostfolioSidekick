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
				Console.WriteLine("1. Scalable Capital (browser)");
				Console.WriteLine("2. Scalable Capital (MCP server)");
				Console.WriteLine("3. Scalable Capital (CLI API)");
				Console.WriteLine("0. Exit");
				var input = Console.ReadLine();
				if (input == null)
				{
					continue;
				}

				SupportedBrokers? broker;
				bool useMcp = false;
				bool useCliApi = false;
				switch (input)
				{
					case "1":
						broker = SupportedBrokers.ScalableCapital;
						break;
					case "2":
						broker = SupportedBrokers.ScalableCapital;
						useMcp = true;
						break;
					case "3":
						broker = SupportedBrokers.ScalableCapital;
						useCliApi = true;
						break;
					case "0":
						Environment.Exit(0);
						return;
					default:
						Console.WriteLine("Invalid input.");
						continue;
				}

				await RunAsync(broker.Value, outputDirectory, useMcp, useCliApi);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public async Task RunAsync(SupportedBrokers broker, string outputDirectory, bool useMcp = false, bool useCliApi = false)
		{
			logger.LogInformation("Starting the scraping process...");
			logger.LogInformation("Broker: {Broker}", broker);
			logger.LogInformation("Output directory: {OutputDirectory}", outputDirectory);

			Dictionary<int, IEnumerable<ActivityWithSymbol>> transactions;
			if (useCliApi && broker == SupportedBrokers.ScalableCapital)
			{
				transactions = await RunCliApiScrapeAsync();
			}
			else if (useMcp && broker == SupportedBrokers.ScalableCapital)
			{
				transactions = await RunMcpScrapeAsync();
			}
			else
			{
				var browser = await playwright.Chromium.ConnectOverCDPAsync("http://localhost:9222");
				var defaultContext = browser.Contexts[0];

				try
				{
					var page = defaultContext.Pages[0];

					switch (broker)
					{
						case SupportedBrokers.ScalableCapital:
							{
								var scraper = new ScalableCapital.Scraper(page, logger);
								transactions = await scraper.ScrapeTransactions();
							}
							break;
						default:
							throw new ArgumentException("Invalid broker entered.");
					}
				}
				finally
				{
					await defaultContext.CloseAsync();
					await browser.CloseAsync();
				}
			}

			CsvHelperService.SaveToCSV(outputDirectory, broker.ToString(), transactions);
			logger.LogInformation("Scraping process completed.");
		}

		private async Task<Dictionary<int, IEnumerable<ActivityWithSymbol>>> RunMcpScrapeAsync()
		{
			using var httpClient = new HttpClient();
			using var tokenProvider = new Mcp.McpTokenProvider(httpClient, logger);
			using var mcpClient = new Mcp.McpClient(httpClient, tokenProvider, logger);
			var scraper = new Mcp.McpScraper(mcpClient, logger);
			return await scraper.ScrapeTransactionsAsync(CancellationToken.None);
		}

		private async Task<Dictionary<int, IEnumerable<ActivityWithSymbol>>> RunCliApiScrapeAsync()
		{
			using var httpClient = new HttpClient();
			using var tokenProvider = new CliApi.CliTokenProvider(httpClient, logger);
			using var client = new CliApi.CliGraphqlClient(httpClient, tokenProvider.Key, tokenProvider);
			var scraper = new CliApi.CliScraper(client, tokenProvider, logger);
			return await scraper.ScrapeTransactionsAsync(CancellationToken.None);
		}
	}
}
