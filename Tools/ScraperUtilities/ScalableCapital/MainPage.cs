using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	public class MainPage(IPage page, ILogger logger)
	{
		internal async Task<TransactionPage> GoToTransactions()
		{
			await page.GotoAsync("https://de.scalable.capital/broker/transactions");

			// Wait for transactions to load
			logger.LogInformation("Waiting for transactions to load...");
			await page.WaitForSelectorAsync("button:text('Export CSV')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

			return new TransactionPage(page, logger);
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