using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ScraperUtilities.CentraalBeheer
{
	public class MainPage(IPage page, Microsoft.Extensions.Logging.ILogger logger)
	{
		internal async Task<TransactionPage> GoToTransactions()
		{
			await page.GotoAsync("https://www.centraalbeheer.nl/mijncb/mijn-producten/beleggen/transacties");

			// Wait for transactions to load
			logger.LogInformation("Waiting for transactions to load...");
			await page.WaitForSelectorAsync("#transacties", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

			return new TransactionPage(page, logger);
		}
	}
}