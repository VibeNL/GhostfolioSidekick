using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using Microsoft.Playwright;
using System.Globalization;

namespace ScraperUtilities.ScalableCapital
{
	internal partial class TransactionPage(IPage page)
	{
		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			await SetExecutedOnly(page);

			// Scroll down the page to load all transactions
			var isScrolling = true;
			var lastUpdate = DateTime.Now;
			while (isScrolling)
			{
				var cnt = await GetTransacionsCount();
				await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
				Thread.Sleep(1000);

				var newCnt = await GetTransacionsCount();
				if (newCnt != cnt)
				{
					lastUpdate = DateTime.Now;
				}

				isScrolling = (DateTime.Now - lastUpdate).TotalSeconds < 5;
			}

			var list = new List<ActivityWithSymbol>();
			int counter = 0;
			foreach (var transaction in await GetTransactions())
			{
				// Every XX transactions, reload the page to avoid memory leaks
				if (counter % 25 == 0)
				{
					await page.ReloadAsync();
					await SetExecutedOnly(page);

					// if transaction is not visible, scroll down
					// We need to do this manually due to lazy loading
					while (!await transaction.IsVisibleAsync())
					{
						await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
						Thread.Sleep(1000);
					}
				}

				// Click
				await transaction.ScrollIntoViewIfNeededAsync();
				await transaction.ClickAsync(new LocatorClickOptions { Position = new Position { X = 2, Y = 2 } }); // avoid clicking any links

				// Wait for the transaction to load
				// Overview text is visible
				await page.WaitForSelectorAsync("div:text('Overview')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

				var generatedTransaction = await ProcessDetails();
				var symbol = await AddSymbol(generatedTransaction);

				if (symbol != null)
				{
					list.Add(symbol);
				}

				// Press Close button
				await page.GetByRole(AriaRole.Button).ClickAsync();

				counter++;
			}

			return list;
		}

		private static async Task SetExecutedOnly(IPage page)
		{
			// Select Executed Status only
			try
			{
				await page.GetByRole(AriaRole.Button).GetByText("Status").ClickAsync();
				await page.GetByTestId("EXECUTED").Locator("div").First.ClickAsync();
				await page.Mouse.ClickAsync(2,2);
			}
			catch (Exception)
			{
			}
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

			var link = page.Locator("[href*=\"/broker/security?\"]").First;
			var name = await link.InnerTextAsync();
			var url = await link.GetAttributeAsync("href");
			if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(name))
			{
				return null;
			}

			var isin = url.Split("isin=")[1];
			return new ActivityWithSymbol
			{
				Activity = generatedTransaction,
				Symbol = isin,
				symbolName = name
			};
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
			if (await page.GetByTestId("icon-DEPOSIT").IsVisibleAsync())
			{
				var dateDeposit = await GetHistoryDate("Deposit settled");

				return new CashDepositWithdrawalActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = dateDeposit,
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			if (await page.GetByTestId("icon-WITHDRAWAL").IsVisibleAsync())
			{
				var dateWithdrawal = await GetHistoryDate("Withdrawal settled");

				return new CashDepositWithdrawalActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = dateWithdrawal,
					TransactionId = await GetField<string>("Transaction reference"),
				};
			}

			// If is Buy or Sell
			var isSaving = await page.GetByTestId("icon-SAVINGS_PLAN").IsVisibleAsync();
			if (isSaving ||
				await page.GetByTestId("icon-BUY").IsVisibleAsync() ||
				await page.GetByTestId("icon-SELL").IsVisibleAsync())
			{
				var isSell = await page.GetByTestId("icon-SELL").IsVisibleAsync();

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
				//await historyNode.HoverAsync();
				var parentHistoryNode = historyNode.Locator("..");
				//await parentHistoryNode.HoverAsync();
				var nodeFromDescription = parentHistoryNode.Locator("div").GetByText(description).First;
				//await nodeFromDescription.HoverAsync();
				var parent = nodeFromDescription.Locator("..");
				//await parent.HoverAsync();
				var dateNode = parent.Locator("div").Nth(1);
				//await dateNode.HoverAsync();
				var text = await dateNode.InnerTextAsync();

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
			var container = page
					.GetByTestId("container")
					.Locator("div")
					.GetByText(description)
					.Locator("..")
					.First;

			var divs = await container.Locator("div").AllAsync();

			var text = await divs[1].InnerTextAsync();
			if (typeof(T) == typeof(decimal))
			{
				text = text.Replace("€", "").Trim();
				return (T)Convert.ChangeType(decimal.Parse(text, NumberStyles.Currency, CultureInfo.InvariantCulture), typeof(T));
			}

			return (T)Convert.ChangeType(text, typeof(T));
		}
	}
}