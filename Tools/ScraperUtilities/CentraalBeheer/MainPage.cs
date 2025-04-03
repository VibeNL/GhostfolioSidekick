using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ScraperUtilities.CentraalBeheer
{
    public class MainPage(IPage page, Microsoft.Extensions.Logging.ILogger logger)
    {
        internal async Task<TransactionPage> GoToTransactions()
        {
            // Navigate to pages
            await page.GotoAsync("https://www.centraalbeheer.nl/mijncb/mijn-producten");
            await page.GotoAsync("https://www.centraalbeheer.nl/mijncb/mijn-producten/beleggen");

            // find button "Bekijken" and press it
            await PressButton("Bekijken");

			// Find button "Afgeronde opdrachten"
			await PressButton("Afgeronde opdrachten");

            // Wait for transactions to load
            logger.LogInformation("Waiting for transactions to load...");
            await page.WaitForSelectorAsync("#transacties", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

            return new TransactionPage(page, logger);
        }

        private async Task PressButton(string text)
        {
            var button = page.Locator($"//a[contains(text(),\"{text}\")]");
            if (button != null)
            {
                await button.ClickAsync();
            }
            else
            {
                logger.LogError("Button '{ButtonText}' not found.", text);
                throw new Exception($"Button '{text}' not found.");
            }
        }
    }
}