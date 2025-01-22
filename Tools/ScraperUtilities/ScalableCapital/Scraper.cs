using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var loginPage = new Login(page, arguments);
            var mainPage = await loginPage.LoginAsync();
			var transactionPage = await mainPage.GoToTransactions();
			var transactions = await transactionPage.ScrapeTransactions();
			return transactions;
		}
    }
}
