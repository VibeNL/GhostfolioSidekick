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
				page.SetDefaultTimeout(5000); // short timeout for this action
				await page.GetByTestId("uc-accept-all-button").ClickAsync();
				page.SetDefaultTimeout(30000); // reset to default
			}
			catch (Exception)
			{ // ignore
			}

			logger.LogInformation("Login successful.");

			// Wait for main page to load
			return new MainPage(page, logger);
		}
	}
}
