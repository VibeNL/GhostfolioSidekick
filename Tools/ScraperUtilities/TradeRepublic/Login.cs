using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.TradeRepublic
{
	public class Login(IPage page, ILogger logger)
	{
		private readonly string[] CookieAcceptList = ["Accept All", "Alles Accepteren"];

		public async Task<MainPage> LoginAsync()
		{
			await page.GotoAsync("https://app.traderepublic.com/login");

			await RemoveCookieBanner();
			await SetEnglish();

			try
			{
				// Wait for portfolio page title
				logger.LogInformation("Waiting for you to login...");
				while (!await page.Locator("span.portfolio__pageTitle").IsVisibleAsync())
				{
					Thread.Sleep(1000);
				}

			}
			catch (Exception)
			{
				// Intentionally left blank
			}

			return new MainPage(page, logger);
		}

		private async Task SetEnglish()
		{
			// <div data-v-010d4a6e="" class="languageSelector">
			logger.LogInformation("Setting language to English...");

			page.SetDefaultTimeout(5000);
			await page.Locator("div.languageSelector").ClickAsync();
			await page.GetByRole(AriaRole.Option, new() { Name = "English" }).ClickAsync();
			page.SetDefaultTimeout(30000);
		}

		private async Task RemoveCookieBanner()
		{
			logger.LogInformation("Removing cookie banner...");

			foreach (var cookieName in CookieAcceptList)
			{
				try
				{
					page.SetDefaultTimeout(5000);
					await page.GetByRole(AriaRole.Button, new() { Name = cookieName }).ClickAsync();
					page.SetDefaultTimeout(30000);
					return;
				}
				catch (Exception)
				{
					// Ignore, continue to the next
				}
			}
		}
	}
}
