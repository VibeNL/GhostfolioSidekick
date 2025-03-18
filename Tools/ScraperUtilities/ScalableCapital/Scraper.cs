using Microsoft.Playwright;

namespace ScraperUtilities.ScalableCapital
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
            _logger.LogInformation("Starting ScalableCapital scraping process...");

            var loginPage = new Login(page, arguments);
            var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			foreach (var account in (await mainPage.GetPortfolios()).Skip(1)) // BUG in SCALABLE IF NO TRANSACTIONS YET IN THE NEW PORTFOLIO
			{
				await mainPage.SwitchToAccount(account);
				var transactionPage = await mainPage.GoToTransactions();
				var transactions = await transactionPage.ScrapeTransactions();
				await transactionPage.GoToMainPage();
				lst.AddRange(transactions);

                _logger.LogInformation($"Scraped {transactions.Count()} transactions for account.");
			}

            _logger.LogInformation("ScalableCapital scraping process completed.");

			return lst;
		}
    }
}
