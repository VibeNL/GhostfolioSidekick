using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ScraperUtilities.ScalableCapital
{
	public class Login(IPage page, ILogger logger)
	{
		public async Task<MainPage> LoginAsync()
		{
			await page.GotoAsync("https://de.scalable.capital/en/secure-login");
			/*await page.Locator("#username").FillAsync(arguments.Username);
			await page.Locator("#password").FillAsync(arguments.Password);

			await page.ClickAsync("button[type='submit']");
			*/

			// Wait for MFA
			logger.LogInformation("Waiting for you to login...");
			while (!await page.GetByTestId("greeting-text").IsVisibleAsync())
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

			// Remove new Scalable banner
			logger.LogInformation("Removing new Scalable banner...");
			try
			{
				await page.ClickAsync("button:text('Start now')");
			}
			catch (Exception)
			{ // ignore
			}

			// Wait for main page to load
			return new MainPage(page, logger);
		}
	}
}
