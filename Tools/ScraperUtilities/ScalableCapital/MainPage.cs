using Microsoft.Playwright;
using System.Collections.Generic;

namespace ScraperUtilities.ScalableCapital
{
	public class MainPage
	{
		private IPage page;

		public MainPage(IPage page)
		{
			this.page = page;
		}
		
		internal async Task<TransactionPage> GoToTransactions()
		{
			await page.GotoAsync("https://de.scalable.capital/broker/transactions");

			// Wait for transactions to load
			await page.WaitForSelectorAsync("button:text('Export CSV')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

			return new TransactionPage(page);
		}

		internal async Task<IReadOnlyCollection<ILocator>> GetPortfolios()
		{
			var brokercards = await page.GetByTestId("broker-card").AllAsync();
			return brokercards;
		}

		internal async Task SwitchToAccount(ILocator account)
		{
			await account.ClickAsync();
		}
	}
}