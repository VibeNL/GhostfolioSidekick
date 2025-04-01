using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ScraperUtilities.CentraalBeheer
{
	internal class Scraper(IPage page, ILogger logger)
	{
		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			var loginPage = new Login(page, logger);
			var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			var transactionPage = await mainPage.GoToTransactions();
			var transactions = await ScrapeTransactionsCommon(transactionPage);
			lst.AddRange(transactions);
			return lst;
		}

		private async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactionsCommon(TransactionPage transactionPage)
		{
			return await transactionPage.ScrapeTransactions();
		}
	}
}
