using Microsoft.Playwright;

namespace ScraperUtilities.ScalableCapital
{
	internal class Scraper
    {
        private readonly IPage page;
		private readonly CommandLineArguments arguments;

		public Scraper(IPage page, CommandLineArguments arguments)
        {
            this.page = page;
			this.arguments = arguments;
		}

        internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
        {
            Console.WriteLine("Starting ScalableCapital scraping process...");

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

                Console.WriteLine($"Scraped {transactions.Count()} transactions for account.");
			}

            Console.WriteLine("ScalableCapital scraping process completed.");

			return lst;
		}
    }
}
