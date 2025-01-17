using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.Playwright;
using System.Globalization;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ScraperUtilities.ScalableCapital
{
	internal partial class TransactionPage(IPage page)
	{
		internal async Task<IEnumerable<Activity>> ScrapeTransactions()
		{
			// Scroll down the page to load all transactions
			var cnt = 0;
			while (cnt != await GetTransacionsCount())
			{
				cnt = await GetTransacionsCount();
				await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
				Thread.Sleep(1000);
			}

			var list = new List<Activity?>();
			foreach (var transaction in await GetTransactions())
			{
				// Click
				await transaction.ClickAsync();

				// Wait for the transaction to load
				// Overview text is visible
				await page.WaitForSelectorAsync("div:text('Overview')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

				list.Add(await ProcessDetails());

				// Press Close button
				await page.Locator("button:text('Close')").ClickAsync();
			}

			return list.Where(x => x is not null);
		}

		private Task<IReadOnlyList<ILocator>> GetTransactions()
		{
			return page.Locator("div[role='list']").AllAsync();
		}

		private Task<int> GetTransacionsCount()
		{
			// Count number of divs with role list
			return page.Locator("div[role='list']").CountAsync();
		}

		private async Task<Activity?> ProcessDetails()
		{
			// If is Deposit or Withdrawal
			if (await page.GetByTestId("icon-DEPOSIT").IsVisibleAsync() ||
				await page.GetByTestId("icon-WITHDRAWAL").IsVisibleAsync())
			{
				return new CashDepositWithdrawalActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = await GetHistoryDate("Deposit settled"),
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			// If is Buy or Sell
			if (await page.GetByTestId("icon-BUY").IsVisibleAsync() ||
				await page.GetByTestId("icon-SAVINGS_PLAN").IsVisibleAsync() ||
				await page.GetByTestId("icon-SELL").IsVisibleAsync())
			{
				var isSell = await page.GetByTestId("icon-SELL").IsVisibleAsync();

				DateTime date;
				try
				{
					date = await GetHistoryDate("Execution confirmed");
				}
				catch (FieldNotFoundException)
				{
					// Cancelled?
					return null;
				}

				var fee = await GetMoneyFieldOptional("Order fee");
				return new BuySellActivity
				{
					Quantity = (isSell ? -1 : 1) * await GetField<decimal>("Executed quantity"),
					UnitPrice = await GetMoneyField("Execution price"),
					TotalTransactionAmount = await GetMoneyField("Market valuation"),
					Fees = fee != null ? [fee] : [],
					Date = date,
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			// If is Distribution
			if (await page.GetByTestId("icon-DIVIDEND").IsVisibleAsync())
			{
				return new CashDepositWithdrawalActivity
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
				var historyNode = page.Locator("div").GetByText("History").First;
				var nodeFromDescription = historyNode.Locator("div").GetByText(description).First;
				var parent = nodeFromDescription.Locator("..");
				var dateNode = parent.Locator("div").Nth(1);
				var text = await dateNode.InnerTextAsync();
				return DateTime.ParseExact(text!, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
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

		private async Task<Money?> GetMoneyFieldOptional(string description)
		{
			try
			{
				return new Money(Currency.EUR, await GetField<decimal>(description));
			}
			catch (FieldNotFoundException)
			{
				return null;
			}
		}

		private async Task<T> GetField<T>(string description)
		{
			var containers = await page.GetByTestId("container").AllAsync();

			foreach (var container in containers)
			{
				var divs = await container.Locator("div").AllAsync();

				if (divs.Count != 2)
				{
					continue;
				}

				// if the first div contains the text
				if (await divs[0].InnerTextAsync() == description)
				{
					return (T)Convert.ChangeType(await divs[1].InnerTextAsync(), typeof(T));
				}
			}

			throw new FieldNotFoundException($"Field '{description}' not found");
		}
	}
}