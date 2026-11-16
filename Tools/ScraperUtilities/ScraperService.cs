using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Tools.ScraperUtilities
{
	public class ScraperService(ILogger<ScraperService> logger, IHostApplicationLifetime lifetime) : IHostedService
	{
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Get output dir
			Console.WriteLine("Enter the output directory path:");
			var outputDirectory = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(outputDirectory))
			{
				logger.LogWarning("No output directory provided; exiting.");
				lifetime.StopApplication();
				return;
			}

			await RunAsync(SupportedBrokers.ScalableCapital, outputDirectory);
			lifetime.StopApplication();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public async Task RunAsync(SupportedBrokers broker, string outputDirectory)
		{
			logger.LogInformation("Starting the scraping process...");
			logger.LogInformation("Broker: {Broker}", broker);
			logger.LogInformation("Output directory: {OutputDirectory}", outputDirectory);

			var transactions = await RunMcpScrapeAsync();
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
	}
}
