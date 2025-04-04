using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CentraalBeheer
{
	public class Login(IPage page, ILogger logger)
	{
		public async Task<MainPage> LoginAsync()
		{
			await page.GotoAsync("https://www.centraalbeheer.nl/_login?returnUrl=/mijncb");

			// Wait for MFA
			logger.LogInformation("Waiting for you to login...");
			while (!await page.Locator("#mijnintro-ingelogd-PageTitleSubTitle").IsVisibleAsync())
			{
				Thread.Sleep(1000);
			}

			// Wait for main page to load
			return new MainPage(page, logger);
		}
	}
}
