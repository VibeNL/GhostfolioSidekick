using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CentraalBeheer
{
	internal class Scraper(IPage page, ILogger logger)
	{
		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			var loginPage = new Login(page, logger);
			var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			var transactionPage = await mainPage.GoToTransactions();
			var transactions = await transactionPage.ScrapeTransactions();
			lst.AddRange(transactions);
			return lst;
		}
	}
}
