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
            { // ignore3129
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
            catch (Exception) { }

            return new MainPage(page);
        }
    }
}
