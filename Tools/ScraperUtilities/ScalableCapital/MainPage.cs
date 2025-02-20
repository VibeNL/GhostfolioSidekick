using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace ScraperUtilities.ScalableCapital
{
	public class MainPage
	{
		private IWebDriver driver;

		public MainPage(IWebDriver driver)
		{
			this.driver = driver;
		}

		internal async Task<TransactionPage> GoToTransactions()
		{
			driver.Navigate().GoToUrl("https://de.scalable.capital/broker/transactions");

			// Wait for transactions to load
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
			wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("button:text('Export CSV')")));

			return new TransactionPage(driver);
		}
	}
}
