using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.TradeRepublic
{
	public class Login(IPage page, ILogger logger)
	{
		public async Task<MainPage> LoginAsync()
		{
			await page.GotoAsync("https://app.traderepublic.com/login");
			await RemoveCookieBanner();

			try
			{
				// Wait for portfolio page title
				logger.LogInformation("Waiting for you to login...");
				while (!await page.Locator("span.portfolio__pageTitle").IsVisibleAsync())
				{
					Thread.Sleep(1000);
				}

			}
			catch (Exception) {
				// Intentionally left blank
			}

			return new MainPage(page, logger);
		}

		private async Task RemoveCookieBanner()
		{
			logger.LogInformation("Removing cookie banner...");
			try
			{
				page.SetDefaultTimeout(5000);
				await page.GetByRole(AriaRole.Button, new() { Name = "Accept All" }).ClickAsync();
				page.SetDefaultTimeout(30000);
			}
			catch (Exception)
			{ // ignore3129
			}

			try
			{
				page.SetDefaultTimeout(5000);
				await page.GetByRole(AriaRole.Button, new() { Name = "Alles accepteren" }).ClickAsync();
				page.SetDefaultTimeout(30000);
			}
			catch (Exception)
			{ // ignore3129
			}
		}
	}
}
