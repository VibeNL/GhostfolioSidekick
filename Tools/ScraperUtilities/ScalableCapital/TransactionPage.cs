using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	internal partial class TransactionPage(IPage page, ILogger logger)
	{
		private const string Description = "Transaction reference";

		private const string Url = "https://de.scalable.capital/cockpit/";

		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			logger.LogInformation("Scraping transactions...");

			await SetExecutedOnly(page);
			await ScrollDown(page);

			var list = new List<ActivityWithSymbol>();
			int counter = 0;
			foreach (var transaction in await GetTransactions())
			{
				logger.LogInformation("Processing transaction {Counter}...", counter);

				// Click on the transaction to open the details
				await transaction.ScrollIntoViewIfNeededAsync();
				await transaction.ClickAsync(new LocatorClickOptions { Position = new Position { X = 2, Y = 2 } }); // avoid clicking any links

				// Wait for the transaction to load
				await page.WaitForSelectorAsync("div:text('Overview')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

				// Process transaction details
				var generatedTransaction = await ProcessDetails();
				if (generatedTransaction == null)
				{
					continue;
				}

				var symbol = await AddSymbol(generatedTransaction);
				if (symbol != null)
				{
					list.Add(symbol);
				}

				logger.LogInformation("Transaction {Counter} processed. Generated {GeneratedTransaction}", counter, generatedTransaction.ToString());

				// Press Close button to close the details
				await page.GetByRole(AriaRole.Button).ClickAsync();

				counter++;
			}

			return list;
		}

		private async Task ScrollDown(IPage page)
		{
			logger.LogInformation("Scrolling down to load all transactions...");

			// Scroll down the page to load all transactions
			var isScrolling = true;
			var lastUpdate = DateTime.UtcNow;
			while (isScrolling)
			{
				var cnt = await GetTransacionsCount();
				await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
				Thread.Sleep(1000);

				var newCnt = await GetTransacionsCount();
				if (newCnt != cnt)
				{
					lastUpdate = DateTime.UtcNow;
				}

				isScrolling = (DateTime.UtcNow - lastUpdate).TotalSeconds < 5;
			}

			logger.LogInformation("All transactions loaded.");
		}

		private async Task SetExecutedOnly(IPage page)
		{
			// Select Executed Status only
			logger.LogInformation("Setting status to Executed only...");
			await page.GetByRole(AriaRole.Button).GetByText("Status").ClickAsync();
			await page.GetByTestId("EXECUTED").Locator("div").First.ClickAsync();

			Thread.Sleep(1000);
			await page.Mouse.ClickAsync(2, 2);
			logger.LogInformation("Status set to Executed only.");
		}

		private async Task<ActivityWithSymbol?> AddSymbol(Activity? generatedTransaction)
		{
			if (generatedTransaction is null)
			{
				return null;
			}

			if (generatedTransaction is CashDepositActivity || generatedTransaction is CashWithdrawalActivity)
			{
				return new ActivityWithSymbol
				{
					Activity = generatedTransaction,
					Symbol = default!,
				};
			}

			var link = page.Locator("[href*=\"/broker/security?\"]").Last;
			var name = await link.InnerTextAsync();
			var url = await link.GetAttributeAsync("href");
			if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(name))
			{
				return null;
			}

			var isin = url.Split(
				["isin=", "&"],
				StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[1];
			return new ActivityWithSymbol
			{
				Activity = generatedTransaction,
				Symbol = isin,
				SymbolName = name
			};
		}

		private Task<IReadOnlyList<ILocator>> GetTransactions()
		{
			return page.GetByTestId(TransactionTestId()).AllAsync();
		}

		private Task<int> GetTransacionsCount()
		{
			// Count number of divs with role list
			return page.GetByTestId(TransactionTestId()).CountAsync();
		}

		private async Task<Activity?> ProcessDetails()
		{
			// If is Deposit or Withdrawal
			if (await page.GetByTestId("icon-DEPOSIT").IsVisibleAsync())
			{
				var dateDeposit = await GetHistoryDate("Deposit settled");

				return new CashDepositActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = dateDeposit,
					TransactionId = await GetField<string>(Description),
				};
			}

			if (await page.GetByTestId("icon-WITHDRAWAL").IsVisibleAsync())
			{
				var dateWithdrawal = await GetHistoryDate("Withdrawal settled");

				return new CashWithdrawalActivity
				{
					Amount = (await GetMoneyField("Amount")).Times(-1),
					Date = dateWithdrawal,
					TransactionId = await GetField<string>(Description),
				};
			}

			// If is Buy or Sell
			var isSaving = await page.GetByTestId("icon-SAVINGS_PLAN").IsVisibleAsync();
			bool isBuy = await page.GetByTestId("icon-BUY").IsVisibleAsync();
			bool isSell = await page.GetByTestId("icon-SELL").IsVisibleAsync();
			if (isSaving ||
				isBuy ||
				isSell)
			{
				var date = await GetHistoryDate("Execution confirmed");
				Money? fee = null;
				if (!isSaving)
				{
					fee = await GetMoneyField("Order fee");
				}

				if (isSell)
				{
					return new SellActivity
					{
						Quantity = await GetField<decimal>("Executed quantity"),
						UnitPrice = await GetMoneyField("Execution price"),
						TotalTransactionAmount = await GetMoneyField("Market valuation"),
						Fees = fee != null ? [new SellActivityFee(fee)] : [],
						Date = date,
						TransactionId = await GetField<string>(Description),
					};
				}

				return new BuyActivity
				{
					Quantity = await GetField<decimal>("Executed quantity"),
					UnitPrice = await GetMoneyField("Execution price"),
					TotalTransactionAmount = await GetMoneyField("Market valuation"),
					Fees = fee != null ? [new BuyActivityFee(fee)] : [],
					Date = date,
					TransactionId = await GetField<string>(Description),
				};
			}

			// If is Distribution
			if (await page.GetByTestId("icon-DIVIDEND").IsVisibleAsync())
			{
				return new DividendActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = await GetHistoryDate("Dividend settled"),
					TransactionId = await GetField<string>(Description),
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
				var parentHistoryNode = historyNode.Locator("..");
				var nodeFromDescription = parentHistoryNode.Locator("div").GetByText(description).First;
				var parent = nodeFromDescription.Locator("..");
				var dateNode = parent.Locator("div").Nth(1);
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

		internal async Task GoToMainPage()
		{
			await page.GotoAsync(Url);
		}

		[GeneratedRegex(".*transaction.*")]
		private static partial Regex TransactionTestId();
	}
}