using Microsoft.Playwright;

namespace ScraperUtilities.ScalableCapital
{
	public class Login(IPage page, CommandLineArguments arguments)
	{
		public async Task<MainPage> LoginAsync()
		{
			await page.GotoAsync("https://de.scalable.capital/en/secure-login");
			await page.Locator("#username").FillAsync(arguments.Username);
			await page.Locator("#password").FillAsync(arguments.Password);

			await page.ClickAsync("button[type='submit']");

			// Wait for MFA
			while (!await page.GetByTestId("greeting-text").IsVisibleAsync())
			{
				Thread.Sleep(1000);
			}

			// Remove cookie banner
			try
			{
				await page.GetByTestId("uc-accept-all-button").ClickAsync();
			}
			catch (Exception)
			{ // ignore
			}

			return new MainPage(page);
		}
	}
}
