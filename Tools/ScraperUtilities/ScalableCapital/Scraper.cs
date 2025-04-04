﻿using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	internal class Scraper(IPage page, ILogger logger)
	{
		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
        {
            var loginPage = new Login(page, logger);
            var mainPage = await loginPage.LoginAsync();

			var lst = new List<ActivityWithSymbol>();
			foreach (var account in (await mainPage.GetPortfolios()).Skip(1)) // BUG in SCALABLE IF NO TRANSACTIONS YET IN THE NEW PORTFOLIO
			{
				await mainPage.SwitchToAccount(account);
				var transactionPage = await mainPage.GoToTransactions();
				var transactions = await transactionPage.ScrapeTransactions();
				await transactionPage.GoToMainPage();
				lst.AddRange(transactions);
			}

			return lst;
		}
    }
}
