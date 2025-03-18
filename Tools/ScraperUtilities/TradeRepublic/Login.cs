using Microsoft.Playwright;

namespace ScraperUtilities.TradeRepublic
{
    public class Login(IPage page, CommandLineArguments arguments)
    {
        public async Task<MainPage> LoginAsync()
        {
            try
            {
                Console.WriteLine("Starting login process...");

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
                    Console.WriteLine($"An error occurred during the login process: {ex.Message}");
                    throw;
                }

                Console.WriteLine("Login process completed successfully.");
                return new MainPage(page);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during the login process: {ex.Message}");
                throw;
            }
        }
    }
}
