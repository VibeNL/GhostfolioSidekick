using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	internal class Scraper(IPage page, ILogger logger)
	{
		internal async Task<Dictionary<int, IEnumerable<ActivityWithSymbol>>> ScrapeTransactions()
		{
			var loginPage = new Login(page, logger);
			var mainPage = await loginPage.LoginAsync();

			var lst = new Dictionary<int, IEnumerable<ActivityWithSymbol>>();
			var i = 0;
			foreach (var account in await mainPage.GetPortfolios())
			{
				await MainPage.SwitchToAccount(account);
				var transactionPage = await mainPage.GoToTransactions();
				var transactions = await transactionPage.ScrapeTransactions();
				await transactionPage.GoToMainPage();
				lst.Add(i++, transactions);
			}

			return lst;
		}
	}
}
