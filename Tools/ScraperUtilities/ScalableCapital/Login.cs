using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace ScraperUtilities.ScalableCapital
{
	public class Login
	{
		private readonly IPage page;
		private readonly CommandLineArguments arguments;
		private readonly ILogger<Login> _logger;

		public Login(IPage page, CommandLineArguments arguments, ILogger<Login> logger)
		{
			this.page = page;
			this.arguments = arguments;
			_logger = logger;
		}

		public async Task<MainPage> LoginAsync()
		{
			try
			{
				_logger.LogInformation("Starting login process...");

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
				catch (Exception ex)
				{
					_logger.LogWarning($"Cookie banner not found: {ex.Message}");
				}

				// Remove new Scalable banner
				try
				{
					await page.ClickAsync("button:text('Start now')");
				}
				catch (Exception ex)
				{
					_logger.LogWarning($"Scalable banner not found: {ex.Message}");
				}

				_logger.LogInformation("Login process completed successfully.");
				return new MainPage(page);
			}
			catch (Exception ex)
			{
				_logger.LogError($"An error occurred during the login process: {ex.Message}");
				throw;
			}
		}
	}
}
