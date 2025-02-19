using Microsoft.Playwright;

namespace ScraperUtilities.TradeRepublic
{
	public class Login(IPage page, CommandLineArguments arguments)
	{
		public async Task<MainPage> LoginAsync()
		{
			await page.GotoAsync("https://app.traderepublic.com/login");

			// Remove cookie banner
			try
			{
				await page.GetByRole(AriaRole.Button, new() { Name = "Accept All" }).ClickAsync();
			}
			catch (Exception)
			{ // ignore
			}

			await page.Locator("#dropdownList__openButton").ClickAsync();


			await page.Locator("#username").FillAsync(arguments.CountryCode);
			await page.Locator("#password").FillAsync(arguments.PhoneNumber);

			await page.ClickAsync("button[type='submit']");

			// Wait for MFA
			while (!await page.GetByTestId("greeting-text").IsVisibleAsync())
			{
				Thread.Sleep(1000);
			}

			

			// Remove new Scalable banner
			try
			{
				await page.ClickAsync("button:text('Start now')");
			}
			catch (Exception)
			{ // ignore
			}

			return new MainPage(page);
		}
	}
}
