using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace ScraperUtilities.TradeRepublic
{
	internal class Scraper
	{
		private readonly IPage page;
		private readonly CommandLineArguments arguments;
		private readonly ILogger<Scraper> _logger;

		public Scraper(IPage page, CommandLineArguments arguments, ILogger<Scraper> logger)
		{
			this.page = page;
			this.arguments = arguments;
			_logger = logger;
		}

		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			_logger.LogInformation("Starting TradeRepublic scraping process...");

			var loginPage = new Login(page, arguments);
			var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			var symbols = await mainPage.ScrapeSymbols();
			var transactionPage = await mainPage.GoToTransactions();
			var transactions = await transactionPage.ScrapeTransactions(symbols);
			lst.AddRange(transactions);

			_logger.LogInformation($"Scraped {transactions.Count()} transactions for TradeRepublic.");

			_logger.LogInformation("TradeRepublic scraping process completed.");

			return lst;
		}
	}
}
