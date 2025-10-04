using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Tools.ScraperUtilities.TradeRepublic
{
	internal partial class TransactionPage(IPage page, ILogger logger)
	{
		internal static readonly string[] sourceArray = [
				"Status", "Transfer",
				"Card payment", "Card refund",
				"Round up", "Saveback", "Savings Plan" ];

		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions(ICollection<SymbolProfile> knownProfiles, string outputDirectory)
		{
			logger.LogInformation("Scraping transactions...");

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
				await page.WaitForSelectorAsync("h3:text('Overview')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

				// Process transaction details
				var generatedTransaction = await ProcessDetails(knownProfiles, outputDirectory);
				if (generatedTransaction == null)
				{
					logger.LogWarning("Transaction {Counter} skipped. No valid activity found.", counter);
				}
				else
				{
					list.Add(generatedTransaction);
					logger.LogInformation("Transaction {Counter} processed. Generated {GeneratedTransaction}", counter, generatedTransaction.Activity.ToString());
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

		private Task<IReadOnlyList<ILocator>> GetTransactions()
		{
			return page.Locator("li[class='timeline__entry']").AllAsync();
		}

		private Task<int> GetTransacionsCount()
		{
			// Count number of divs with role list
			return page.Locator("li[class='timeline__entry']").CountAsync();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Scraper logic is complex")]
		private async Task<ActivityWithSymbol?> ProcessDetails(ICollection<SymbolProfile> knownProfiles, string outputDirectory)
		{
			var table = await ParseTable(0);
			var status = table.FirstOrDefault(x => sourceArray.Contains(x.Item1)).Item2;

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
				logger.LogWarning("Failed to parse date: {DateString}", dateString);
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
							Amount = ParseMoneyFromHeader(headerText),
							Date = parsedTime,
							TransactionId = GenerateTransactionId(time, table),
							Description = headerText,
						},
						Symbol = default!,
					};
				}

				var annualRate = table.FirstOrDefault(x => x.Item1 == "Annual rate").Item2;
				if (annualRate != null)
				{
					return new ActivityWithSymbol
					{
						Activity = new InterestActivity
						{
							Amount = ParseMoneyFromHeader(headerText),
							Date = parsedTime,
							TransactionId = GenerateTransactionId(time, table),
							Description = headerText,
						},
						Symbol = default!,
					};
				}

				return new ActivityWithSymbol
				{
					Activity = new CashDepositActivity
					{
						Amount = ParseMoneyFromHeader(headerText),
						Date = parsedTime,
						TransactionId = GenerateTransactionId(time, table),
						Description = headerText,
					},
					Symbol = default!,
				};
			}

			if (headerText.Contains("You sent") || headerText.Contains("You spent"))
			{
				return new ActivityWithSymbol
				{
					Activity = new CashWithdrawalActivity
					{
						Amount = ParseMoneyFromHeader(headerText),
						Date = parsedTime,
						TransactionId = GenerateTransactionId(time, table),
						Description = headerText,
					},
					Symbol = default!,
				};
			}

			var rewards = headerText.Contains("Your reward of");
			var saving = headerText.Contains("You saved");
			if (headerText.Contains("You invested") || saving || rewards)
			{
				var asset = table.FirstOrDefault(x => x.Item1 == "Asset").Item2;

				var symbol = knownProfiles
				.FirstOrDefault(x => x.ISIN == asset || x.Name == asset);

				if (symbol == null)
				{
					logger.LogWarning("Symbol not found: {Asset}", asset);
					return null;
				}

				// Download the attached document if available
				var links = await page.Locator("div[class='detailDocuments__entry']").AllAsync();
				int counter = 1;
				foreach (var item in links)
				{
					// open the link in a new tab
					var countPages = page.Context.Pages.Count;
					await item.ClickAsync();

					while (page.Context.Pages.Count <= countPages)
					{
						await Task.Delay(100);
					}

					var newPage = page.Context.Pages.LastOrDefault();
					if (newPage != null)
					{
						try
						{
							// Wait for the new page to load
							await newPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
						}
						catch
						{
							// Ignore for now
						}

						// Get Url
						var url = newPage.Url;

						// Download from url
						logger.LogInformation("Downloading document from {Url}", url);
						var fileName = $"{symbol.ISIN} {parsedTime:yyyy-MM-dd-HH-mm-ss} {counter++}.pdf";
						var directory = Path.Combine(outputDirectory, "TradeRepublic");
						var filePath = Path.Combine(directory, fileName);
						if (!Directory.Exists(directory))
						{
							Directory.CreateDirectory(directory);
						}

						using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
						{
							// Get manually from url
							var response = await newPage.Context.APIRequest.GetAsync(url);
							if (response.Ok)
							{
								var body = await response.BodyAsync();
								// Write the response body to the file
								await fileStream.WriteAsync(body);
							}
							else
							{
								logger.LogWarning("Failed to download document from {Url}. Status code: {StatusCode}", url, response.Status);
							}
						}

						// Close the new page
						await newPage.CloseAsync();
					}
				}
			}

			return null;
		}

		private static Money ParseMoneyFromHeader(string headerText)
		{
			// Parse the value from strings like 'You received €1,105.00'
			// Or 'You added €5.00 via Direct Debit'
			var euroPattern = new Regex(@"€\s?([\d,\.]+)", RegexOptions.None, TimeSpan.FromSeconds(1));

			// Parse the value from strings like 'You received 74.17 EUR'
			var eurPattern = new Regex(@"([\d,\.]+)\s*EUR", RegexOptions.None, TimeSpan.FromSeconds(1));

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
		private static string GenerateTransactionId(string time, List<(string, string)> table)
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