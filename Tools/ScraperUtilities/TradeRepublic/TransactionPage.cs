using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using Microsoft.Playwright;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ScraperUtilities.TradeRepublic
{
	internal partial class TransactionPage(IPage page)
	{
		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			await ScrollDown(page);

			var list = new List<ActivityWithSymbol>();
			int counter = 0;
			foreach (var transaction in await GetTransactions())
			{
				// Click on the transaction to open the details
				await transaction.ScrollIntoViewIfNeededAsync();

				var location = await transaction.BoundingBoxAsync();
				await transaction.ClickAsync(new LocatorClickOptions { Position = new Position { X = 2, Y = 2 } }); // avoid clicking any links

				// Wait for the transaction to load
				await page.WaitForSelectorAsync("h3:text('Overview')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

				// Process transaction details
				var generatedTransaction = await ProcessDetails();
				var symbol = await AddSymbol(generatedTransaction);

				if (symbol != null)
				{
					list.Add(symbol);
				}

				// Press Close button to close the details
				var closeButtons = await page.Locator("svg[class='closeIcon']").AllAsync();
				await closeButtons.Skip(1).First().ClickAsync();
				
				counter++;
			}

			return list;
		}

		private async Task ScrollDown(IPage page)
		{
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

			var isin = url.Split(
				["isin=", "&"],
				StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[1];
			return new ActivityWithSymbol
			{
				Activity = generatedTransaction,
				Symbol = isin,
				symbolName = name
			};
		}

		private Task<IReadOnlyList<ILocator>> GetTransactions()
		{
			return page.Locator("li[class='timeline__entry']").AllAsync();
		}

		private Task<int> GetTransacionsCount()
		{
			// Count number of divs with role list
			return page.Locator("li[class='timeline__entry']").CountAsync();
		}

		private async Task<Activity?> ProcessDetails()
		{
			var table = await ParseTable();
			var status = table.FirstOrDefault(x => x.Item1 == "Status").Item2;

			var completedStatus = new string[] { "Completed",  "Executed", };

			if (table.Count == 0 || !completedStatus.Contains(status))
			{
				return null;
			}

			// Find the h2 with class detailHeader__heading
			var header = page.Locator("h2[class='detailHeader__heading']").First;
			var headerText = await header.InnerTextAsync();

			// Find the p with clas detailHeader__subheading -time
			var time = await page.Locator("p[class='detailHeader__subheading -time']").First.InnerHTMLAsync();
			var parsedTime = DateTime.ParseExact(time.Replace(" at", string.Empty), "dd MMMM HH:mm", CultureInfo.InvariantCulture);

			// Depending on the text in the header, we can determine the type of transaction
			if (headerText.Contains("You received"))
			{
				return new CashDepositWithdrawalActivity
				{
					Amount = await ParseMoneyFromHeader(headerText),
					Date = parsedTime,
					TransactionId = GenerateTransactionId(time, table),
				};
			}

			if (headerText.Contains("You sent") || headerText.Contains("You spent"))
			{
				return new CashDepositWithdrawalActivity
				{
					Amount = (await ParseMoneyFromHeader(headerText)).Times(-1),
					Date = parsedTime,
					TransactionId = GenerateTransactionId(time, table),
				};
			}

			if (headerText.Contains("You invested"))
			{
				var quantity = table.FirstOrDefault(x => x.Item1 == "Shares").Item2;
				var unitPrice = table.FirstOrDefault(x => x.Item1 == "Share pricee").Item2;
				var fee = table.FirstOrDefault(x => x.Item1 == "Fee").Item2;
				var total = table.FirstOrDefault(x => x.Item1 == "Total").Item2;
				var orderType = table.FirstOrDefault(x => x.Item1 == "Order Type").Item2;

				var fees = new List<Money>();
				if (fee != "Free")
				{
					fees.Add(new Money(Currency.EUR, ParseMoney(fee)));
				}

				return new BuySellActivity
				{
					Quantity = ParseMoney(quantity),
					UnitPrice = new Money(Currency.EUR, ParseMoney(unitPrice)),
					Date = parsedTime,
					TransactionId = GenerateTransactionId(time, table),
				};
			}


			return null;
		}

		private decimal ParseMoney(string money)
		{
			return decimal.Parse(money, NumberStyles.Currency, CultureInfo.InvariantCulture);
		}

		private async Task<Money> ParseMoneyFromHeader(string headerText)
		{
			// Parse the value from strings like 'You received €1,105.00'
			var amount = headerText.Split("€")[1].Trim();
			return new Money(
				Currency.EUR,
				decimal.Parse(amount, NumberStyles.Currency, CultureInfo.InvariantCulture));
		}
		private string GenerateTransactionId(string time, List<(string, string)> table)
		{
			return time + string.Join("|", table.Select(x => x.Item2));
		}

		private async Task<List<(string, string)>> ParseTable()
		{
			// Find div with class detailTable
			var detailTable = page.Locator("div[class='detailTable']").First;

			// Find all rows in the table
			var rows = await detailTable.Locator("div[class='detailTable__row']").AllAsync();

			// Each row contains a dt and dd element
			var list = new List<(string, string)>();
			foreach (var row in rows)
			{
				var dt = await row.Locator("dt").First.InnerTextAsync();
				var dd = await row.Locator("dd").First.InnerTextAsync();
				list.Add((dt, dd));
			}

			return list;
		}
	}
}