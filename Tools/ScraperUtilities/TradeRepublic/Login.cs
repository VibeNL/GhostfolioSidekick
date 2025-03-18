using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace ScraperUtilities.TradeRepublic
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

                await page.GotoAsync("https://app.traderepublic.com/login");

                // Remove cookie banner
                try
                {
                    await page.GetByRole(AriaRole.Button, new() { Name = "Accept All" }).ClickAsync();
                }
                catch (Exception)
                { // ignore
                }

                try
                {
                    // Select phone number
                    await page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();
                    await page.Locator($"#areaCode-\\{arguments.CountryCode}").ClickAsync();
                    await page.Locator("#loginPhoneNumber__input").FillAsync(arguments.PhoneNumber);
                    await page.ClickAsync("button[type='submit']");

                    // Set pin
                    await page.Keyboard.TypeAsync(arguments.PinCode);

                    // Wait for portfolio page title
                    while (!await page.Locator("span.portfolio__pageTitle").IsVisibleAsync())
                    {
                        Thread.Sleep(1000);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred during the login process: {ex.Message}");
                    throw;
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
