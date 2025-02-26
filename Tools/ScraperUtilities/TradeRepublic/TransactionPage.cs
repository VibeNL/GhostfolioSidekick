using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Playwright;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ScraperUtilities.TradeRepublic
{
	internal partial class TransactionPage(IPage page)
	{
		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions(ICollection<SymbolProfile> knownProfiles)
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
				var generatedTransaction = await ProcessDetails(knownProfiles);
				if (generatedTransaction != null)
				{
					list.Add(generatedTransaction);
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

		private Task<IReadOnlyList<ILocator>> GetTransactions()
		{
			return page.Locator("li[class='timeline__entry']").AllAsync();
		}

		private Task<int> GetTransacionsCount()
		{
			// Count number of divs with role list
			return page.Locator("li[class='timeline__entry']").CountAsync();
		}

		private async Task<ActivityWithSymbol?> ProcessDetails(ICollection<SymbolProfile> knownProfiles)
		{
			var table = await ParseTable(0);
			var status = table.FirstOrDefault(x => x.Item1 == "Status").Item2;

			var completedStatus = new string[] { "Completed", "Executed", };

			if (table.Count == 0 || !completedStatus.Contains(status))
			{
				return null;
			}

			// Find the h2 with class detailHeader__heading
			var header = page.Locator("h2[class='detailHeader__heading']").First;
			var headerText = await header.InnerTextAsync();

			// Find the p with clas detailHeader__subheading -time
			var time = await page.Locator("p[class='detailHeader__subheading -time']").First.InnerHTMLAsync();
			string dateString = time.Replace(" at", string.Empty);

			if (!DateTime.TryParseExact(dateString, "dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedTime) && 
				!DateTime.TryParseExact(dateString, "dd MMMM HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedTime))
			{
				// TODO logging
				return null;
			}

			// Depending on the text in the header, we can determine the type of transaction
			if (headerText.Contains("You received") || headerText.Contains("You added"))
			{
				var @event = table.FirstOrDefault(x => x.Item1 == "Event").Item2;

				if (@event == "Dividend")
				{
					return new ActivityWithSymbol
					{
						Activity = new DividendActivity
						{
							Amount = await ParseMoneyFromHeader(headerText),
							Date = parsedTime,
							TransactionId = GenerateTransactionId(time, table),
							Description = headerText,
						}
					};
				}

				var annualRate = table.FirstOrDefault(x => x.Item1 == "Annual rate").Item2;
				if (annualRate != null)
				{
					return new ActivityWithSymbol
					{
						Activity = new InterestActivity
						{
							Amount = await ParseMoneyFromHeader(headerText),
							Date = parsedTime,
							TransactionId = GenerateTransactionId(time, table),
							Description = headerText,
						}
					};
				}

				return new ActivityWithSymbol
				{
					Activity = new CashDepositWithdrawalActivity
					{
						Amount = await ParseMoneyFromHeader(headerText),
						Date = parsedTime,
						TransactionId = GenerateTransactionId(time, table),
						Description = headerText,
					}
				};
			}

			if (headerText.Contains("You sent") || headerText.Contains("You spent"))
			{
				return new ActivityWithSymbol
				{
					Activity = new CashDepositWithdrawalActivity
					{
						Amount = (await ParseMoneyFromHeader(headerText)).Times(-1),
						Date = parsedTime,
						TransactionId = GenerateTransactionId(time, table),
						Description = headerText,
					}
				};
			}

			var rewards = headerText.Contains("Your reward of");
			var saving = headerText.Contains("You saved");
			if (headerText.Contains("You invested") || saving || rewards)
			{
				var transactionTable = await ParseTable(saving ? 2 : 1);
				var quantity = transactionTable.FirstOrDefault(x => x.Item1 == "Shares").Item2;
				var unitPrice = transactionTable.FirstOrDefault(x => x.Item1 == "Share price").Item2;
				var fee = transactionTable.FirstOrDefault(x => x.Item1 == "Fee").Item2;
				var total = transactionTable.FirstOrDefault(x => x.Item1 == "Total").Item2;
				var orderType = table.FirstOrDefault(x => x.Item1 == "Order Type").Item2;
				var asset = table.FirstOrDefault(x => x.Item1 == "Asset").Item2;

				var fees = new List<Money>();
				if (fee != "Free")
				{
					fees.Add(new Money(Currency.EUR, ParseMoney(fee)));
				}

				var symbol = knownProfiles
					.FirstOrDefault(x => x.ISIN == asset || x.Name == asset);

				if (symbol == null)
				{
					// TODO logging
					return null;
				}

				if (rewards)
				{
					return new ActivityWithSymbol
					{
						Activity = new GiftAssetActivity
						{
							Quantity = ParseMoney(quantity),
							UnitPrice = new Money(Currency.EUR, ParseMoney(unitPrice)),
							Date = parsedTime,
							TransactionId = GenerateTransactionId(time, table),
							Description = headerText,
						},
						Symbol = symbol.ISIN,
						symbolName = symbol.Name,
					};
				}

				return new ActivityWithSymbol
				{
					Activity = new BuySellActivity
					{
						Quantity = ParseMoney(quantity),
						UnitPrice = new Money(Currency.EUR, ParseMoney(unitPrice)),
						Date = parsedTime,
						TransactionId = GenerateTransactionId(time, table),
						TotalTransactionAmount = new Money(Currency.EUR, ParseMoney(total)),
						Description = headerText,
					},
					Symbol = symbol.ISIN,
					symbolName = symbol.Name,
				};
			}

			return null;
		}

		private decimal ParseMoney(string money)
		{
			// Parse the value from strings like '€1,105.00'
			return decimal.Parse(money.Replace("€", string.Empty), NumberStyles.Currency, CultureInfo.InvariantCulture);
		}

		private static async Task<Money> ParseMoneyFromHeader(string headerText)
		{
			// Parse the value from strings like 'You received €1,105.00'
			// Or 'You added €5.00 via Direct Debit'
			var euroPattern = new Regex(@"€\s?([\d,\.]+)");
			
			// Parse the value from strings like 'You received 74.17 EUR'
			var eurPattern = new Regex(@"([\d,\.]+)\s*EUR");

			var euroMatch = euroPattern.Match(headerText);
			if (euroMatch.Success)
			{
				var amount = euroMatch.Groups[1].Value;
				return new Money(
					Currency.EUR,
					decimal.Parse(amount, NumberStyles.Currency, CultureInfo.InvariantCulture));
			}

			var eurMatch = eurPattern.Match(headerText);
			if (eurMatch.Success)
			{
				var amount = eurMatch.Groups[1].Value;
				return new Money(
					Currency.EUR,
					decimal.Parse(amount, NumberStyles.Currency, CultureInfo.InvariantCulture));
			}

			return new Money();
		}
		private string GenerateTransactionId(string time, List<(string, string)> table)
		{
			return time + string.Join("|", table.Select(x => x.Item2));
		}

		private async Task<List<(string, string)>> ParseTable(int number)
		{
			// Find div with class detailTable
			var detailTables = await page.Locator("div[class='detailTable']").AllAsync();
			var detailTable = detailTables[number];

			var list = new List<(string, string)>();
			// Find all rows in the table
			var rows = await detailTable.Locator("div[class='detailTable__row']").AllAsync();

			// Each row contains a dt and dd element
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