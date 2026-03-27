using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	public class MainPage(IPage page, ILogger logger)
	{
		internal async Task<TransactionPage> GoToTransactions()
		{
			// Find link with href containing 'transactions' by using Locator
			await page.Locator("a[href*='transactions']").ClickAsync();

			// Wait for transactions to load
			logger.LogInformation("Waiting for transactions to load...");
			await page.WaitForSelectorAsync("input[placeholder='Search transactions']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

			return page.Url.Contains("/broker")
				? new BrokerTransactionPage(page, logger)
				: new InterestTransactionPage(page, logger);
		}

		internal async Task<IReadOnlyCollection<ILocator>> GetPortfolios()
		{
			var products = await page.GetByLabel("Products").AllAsync();
			var brokercards = await products[0].GetByRole(AriaRole.Listitem).AllAsync();
			return brokercards.Reverse().ToList(); // Debug
		}

		internal static async Task SwitchToAccount(ILocator account)
		{
			await account.ClickAsync();
		}
	}
}