using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.TradeRepublic
{
	internal class Scraper(IPage page, ILogger logger, string outputDirectory)
	{
		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			var loginPage = new Login(page, logger);
			var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			var symbols = await mainPage.ScrapeSymbols();
			var transactionPage = await mainPage.GoToTransactions();
			var transactions = await transactionPage.ScrapeTransactions(symbols, outputDirectory);
			lst.AddRange(transactions);
			return lst;
		}
	}
}
