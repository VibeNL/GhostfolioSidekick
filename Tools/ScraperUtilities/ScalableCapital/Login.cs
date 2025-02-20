using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace ScraperUtilities.ScalableCapital
{
	public class Login
	{
		private readonly IWebDriver driver;
		private readonly CommandLineArguments arguments;

		public Login(IWebDriver driver, CommandLineArguments arguments)
		{
			this.driver = driver;
			this.arguments = arguments;
		}

		public async Task<MainPage> LoginAsync()
		{
			driver.Navigate().GoToUrl("https://de.scalable.capital/en/secure-login");

			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
			wait.Until(ExpectedConditions.ElementIsVisible(By.Id("username"))).SendKeys(arguments.Username);
			driver.FindElement(By.Id("password")).SendKeys(arguments.Password);
			driver.FindElement(By.CssSelector("button[type='submit']")).Click();

			// Wait for MFA
			while (!driver.FindElement(By.CssSelector("[data-testid='greeting-text']")).Displayed)
			{
				await Task.Delay(1000);
			}

			// Remove cookie banner
			try
			{
				driver.FindElement(By.CssSelector("[data-testid='uc-accept-all-button']")).Click();
			}
			catch (NoSuchElementException)
			{ // ignore
			}

			return new MainPage(driver);
		}
	}
}
