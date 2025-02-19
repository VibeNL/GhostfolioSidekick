using Microsoft.Playwright;

namespace ScraperUtilities.TradeRepublic
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
			await page.GotoAsync("https://app.traderepublic.com/profile/transactions");

			// Wait for transactions to load
			await page.WaitForSelectorAsync("button:text('Withdraw')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

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