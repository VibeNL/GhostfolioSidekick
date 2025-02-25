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
			var loginPage = new Login(page, arguments);
			var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			var transactionPage = await mainPage.GoToTransactions();
			var transactions = await transactionPage.ScrapeTransactions();
			lst.AddRange(transactions);
			return lst;
		}
	}
}
