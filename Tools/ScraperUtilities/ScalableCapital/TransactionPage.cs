using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Globalization;

namespace ScraperUtilities.ScalableCapital
{
	internal partial class TransactionPage
	{
		private readonly IWebDriver driver;

		public TransactionPage(IWebDriver driver)
		{
			this.driver = driver;
		}

		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			await SetExecutedOnly();
			await ScrollDown();

			var list = new List<ActivityWithSymbol>();
			int counter = 0;
			foreach (var transaction in GetTransactions())
			{
				await NavigateToTransaction(counter, transaction);

				// Click on the transaction to open the details
				((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", transaction);
				transaction.Click();

				// Wait for the transaction to load
				var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
				wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//div[contains(text(), 'Overview')]")));

				// Process transaction details
				var generatedTransaction = await ProcessDetails();
				var symbol = await AddSymbol(generatedTransaction);

				if (symbol != null)
				{
					list.Add(symbol);
				}

				// Press Close button to close the details
				driver.FindElement(By.CssSelector("button[aria-label='Close']")).Click();

				counter++;
			}

			return list;
		}

		private async Task NavigateToTransaction(int counter, IWebElement transaction)
		{
			// Every XX transactions, reload the page to avoid memory leaks
			if (counter % 25 == 0)
			{
				driver.Navigate().Refresh();
				await SetExecutedOnly();

				// if transaction is not visible, scroll down
				// We need to do this manually due to lazy loading
				while (!transaction.Displayed)
				{
					((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
					Thread.Sleep(1000);
				}
			}
		}

		private async Task ScrollDown()
		{
			// Scroll down the page to load all transactions
			var isScrolling = true;
			var lastUpdate = DateTime.Now;
			while (isScrolling)
			{
				var cnt = GetTransacionsCount();
				((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
				Thread.Sleep(1000);

				var newCnt = GetTransacionsCount();
				if (newCnt != cnt)
				{
					lastUpdate = DateTime.Now;
				}

				isScrolling = (DateTime.Now - lastUpdate).TotalSeconds < 5;
			}
		}

		private async Task SetExecutedOnly()
		{
			// Select Executed Status only
			driver.FindElement(By.XPath("//button[contains(text(), 'Status')]")).Click();
			driver.FindElement(By.CssSelector("[data-testid='EXECUTED'] div")).Click();

			Thread.Sleep(1000);
			driver.FindElement(By.CssSelector("body")).Click();
		}

		private async Task<ActivityWithSymbol?> AddSymbol(Activity? generatedTransaction)
		{
			if (generatedTransaction is null)
			{
				return null;
			}

			if (generatedTransaction is CashDepositWithdrawalActivity)
			{
				return new ActivityWithSymbol
				{
					Activity = generatedTransaction,
				};
			}

			var link = driver.FindElement(By.CssSelector("[href*=\"/broker/security?\"]"));
			var name = link.Text;
			var url = link.GetAttribute("href");
			if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(name))
			{
				return null;
			}

			var isin = url.Split(
				["isin=","&"],
				StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[1];
			return new ActivityWithSymbol
			{
				Activity = generatedTransaction,
				Symbol = isin,
				symbolName = name
			};
		}

		private IReadOnlyList<IWebElement> GetTransactions()
		{
			return driver.FindElements(By.CssSelector("div[role='list']"));
		}

		private int GetTransacionsCount()
		{
			// Count number of divs with role list
			return driver.FindElements(By.CssSelector("div[role='list']")).Count;
		}

		private async Task<Activity?> ProcessDetails()
		{
			// If is Deposit or Withdrawal
			if (driver.FindElement(By.CssSelector("[data-testid='icon-DEPOSIT']")).Displayed)
			{
				var dateDeposit = await GetHistoryDate("Deposit settled");

				return new CashDepositWithdrawalActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = dateDeposit,
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			if (driver.FindElement(By.CssSelector("[data-testid='icon-WITHDRAWAL']")).Displayed)
			{
				var dateWithdrawal = await GetHistoryDate("Withdrawal settled");

				return new CashDepositWithdrawalActivity
				{
					Amount = (await GetMoneyField("Amount")).Times(-1),
					Date = dateWithdrawal,
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			// If is Buy or Sell
			var isSaving = driver.FindElement(By.CssSelector("[data-testid='icon-SAVINGS_PLAN']")).Displayed;
			bool isBuy = driver.FindElement(By.CssSelector("[data-testid='icon-BUY']")).Displayed;
			bool isSell = driver.FindElement(By.CssSelector("[data-testid='icon-SELL']")).Displayed;
			if (isSaving ||
				isBuy ||
				isSell)
			{
				DateTime date = DateTime.MinValue;
				var dateConfirmedTask = GetHistoryDate("Execution confirmed");
				var dateRejectedTask = GetHistoryDate("Order rejected");
				var dateCancelledTask = GetHistoryDate("Order cancelled");

				var hasDate = false;
				while (!hasDate)
				{
					if (dateConfirmedTask.IsCompletedSuccessfully)
					{
						date = dateConfirmedTask.Result;
						hasDate = true;
					}
					else if (dateRejectedTask.IsCompletedSuccessfully)
					{
						// Order rejected
						return null;
					}
					else if (dateCancelledTask.IsCompletedSuccessfully)
					{
						// Order cancelled
						return null;
					}
					else if (
						dateConfirmedTask.IsCompleted &&
						dateRejectedTask.IsCompleted &&
						dateCancelledTask.IsCompleted)
					{
						throw new FieldNotFoundException("Field \"Execution confirmed\" not found");
					}
					else
					{
						Thread.Sleep(100);
					}
				}

				Money? fee = null;
				if (!isSaving)
				{
					fee = await GetMoneyField("Order fee");
				}

				return new BuySellActivity
				{
					Quantity = (isSell ? -1 : 1) * await GetField<decimal>("Executed quantity"),
					UnitPrice = await GetMoneyField("Execution price"),
					TotalTransactionAmount = await GetMoneyField("Market valuation"),
					Fees = fee != null ? [new BuySellActivityFee(fee)] : [],
					Date = date,
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			// If is Distribution
			if (driver.FindElement(By.CssSelector("[data-testid='icon-DIVIDEND']")).Displayed)
			{
				return new DividendActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = await GetHistoryDate("Dividend settled"),
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			return null;
		}

		private async Task<DateTime> GetHistoryDate(string description)
		{
			try
			{
				// find the div with the first child containing the text History
				var historyNode = driver.FindElement(By.XPath("//div[contains(text(), 'History')]"));
				var parentHistoryNode = historyNode.FindElement(By.XPath(".."));
				var nodeFromDescription = parentHistoryNode.FindElement(By.XPath($"//div[contains(text(), '{description}')]"));
				var parent = nodeFromDescription.FindElement(By.XPath(".."));
				var dateNode = parent.FindElement(By.XPath("div[2]"));
				var text = dateNode.Text;

				if (DateTime.TryParseExact(text!, "dd MMM yyyy, HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dateTime))
				{
					return dateTime;
				}

				if (DateTime.TryParseExact(text!, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTime))
				{
					return dateTime;
				}

				throw new FieldNotFoundException($"Field '{description}' not found");
			}
			catch (Exception)
			{
				throw new FieldNotFoundException($"Field '{description}' not found");
			}
		}

		private async Task<Money> GetMoneyField(string description)
		{
			return new Money(Currency.EUR, await GetField<decimal>(description));
		}

		private async Task<T> GetField<T>(string description)
		{
			var container = driver.FindElement(By.XPath($"//div[contains(text(), '{description}')]/.."));
			var divs = container.FindElements(By.TagName("div"));

			var text = divs[1].Text;
			if (typeof(T) == typeof(decimal))
			{
				text = text.Replace("€", "").Trim();
				return (T)Convert.ChangeType(decimal.Parse(text, NumberStyles.Currency, CultureInfo.InvariantCulture), typeof(T));
			}

			return (T)Convert.ChangeType(text, typeof(T));
		}
	}
}
