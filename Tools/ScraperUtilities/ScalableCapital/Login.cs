using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	public class Login(IPage page, ILogger logger)
	{
		public async Task<MainPage> LoginAsync()
		{
			await page.GotoAsync("https://de.scalable.capital/en/secure-login");
			
			// Wait for MFA
			logger.LogInformation("Waiting for you to login...");
			while (!await page.GetByTestId("large-price").IsVisibleAsync())
			{
				Thread.Sleep(1000);
			}

			// Remove cookie banner
			logger.LogInformation("Removing cookie banner...");
			try
			{
				await page.GetByTestId("uc-accept-all-button").ClickAsync();
			}
			catch (Exception)
			{ // ignore
			}

			// Wait for main page to load
			return new MainPage(page, logger);
		}
	}
}
