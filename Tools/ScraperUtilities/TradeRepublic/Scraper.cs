using Microsoft.Playwright;

namespace ScraperUtilities.TradeRepublic
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
			Console.WriteLine("Starting TradeRepublic scraping process...");

			var loginPage = new Login(page, arguments);
			var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			var symbols = await mainPage.ScrapeSymbols();
			var transactionPage = await mainPage.GoToTransactions();
			var transactions = await transactionPage.ScrapeTransactions(symbols);
			lst.AddRange(transactions);

			Console.WriteLine($"Scraped {transactions.Count()} transactions for TradeRepublic.");

			Console.WriteLine("TradeRepublic scraping process completed.");

			return lst;
		}
	}
}
