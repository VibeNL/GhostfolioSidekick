using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CentraalBeheer
{
	public class MainPage(IPage page, ILogger logger)
	{
		internal async Task<TransactionPage> GoToTransactions()
		{
			// Click on Beleggingsopdracht
			await page.Locator("a[href*='/mijncb/mijn-producten/beleggen']").ClickAsync();

			// Click on Beleggingsopdracht
			await page.Locator("a[href*='/mijncb/mijn-producten/beleggen/beleggingsrekening-inzien']").ClickAsync();

			// Clock on Afgeronde opdrachten
			await page.Locator("a[href*='/mijncb/mijn-producten/beleggen/transacties']").ClickAsync();

			// Wait for transactions to load
			logger.LogInformation("Waiting for transactions to load...");
			await page.WaitForSelectorAsync("#transacties", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

			return new TransactionPage(page, logger);
		}
	}
}