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

		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions(string outputDirectory)
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
				await DownloadFiles(outputDirectory);

				// Press Close button to close the details
				var closeButtons = await page.Locator("svg[class='closeIcon']").AllAsync();
				await closeButtons[1].ClickAsync();

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

		private async Task DownloadFiles(string outputDirectory)
		{
			var table = await ParseTable(0);
			var status = table.FirstOrDefault(x => sourceArray.Contains(x.Item1)).Item2;

			var completedStatus = new string[] { "Completed", "Executed", };

			if (table.Count == 0 || !completedStatus.Contains(status))
			{
				return;
			}

			// Find the h2 with class detailHeader__heading
			var header = page.Locator("h2[class='detailHeader__heading']").First;
			var headerText = await header.InnerTextAsync();

			DateTime parsedTime = DateTime.MinValue;

			var hasTimeSubheading = false;
			try
			{
				await page.WaitForSelectorAsync(
					"p[class='detailHeader__subheading -time']",
					new PageWaitForSelectorOptions { Timeout = 200 }
				);
				hasTimeSubheading = await page.Locator("p[class='detailHeader__subheading -time']").CountAsync() > 0;
			}
			catch (TimeoutException)
			{
				hasTimeSubheading = false;
			}

			if (hasTimeSubheading)
			{
				// Get the date
				try
				{
					var timeb = await page.Locator("p[class='detailHeader__subheading -time']").First.InnerHTMLAsync();
					string dateString = timeb.Replace(" at", string.Empty);

					if (!DateTime.TryParseExact(dateString, "dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedTime))
					{
						DateTime.TryParseExact(dateString, "dd MMMM HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedTime);
					}
				}
				catch
				{
					// Ignore
				}
			}

			if (parsedTime == DateTime.MinValue)
			{
				try
				{
					var dateString = await page.Locator("p[class='detailHeader__subheading -subtitle']").First.InnerHTMLAsync();
					dateString = dateString.Replace("· ", string.Empty);
					// Dec 2 · 09:02
					DateTime.TryParseExact(dateString, "MMM d HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedTime);

				}
				catch
				{
					// Ignore
				}
			}

			if (parsedTime == DateTime.MinValue)
			{
				logger.LogWarning("Could not parse date for transaction: {HeaderText}", headerText);
				return;
			}


			var asset = table.FirstOrDefault(x => x.Item1 == "Asset").Item2;

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

				var newPage = page.Context.Pages.Count > countPages ? page.Context.Pages[^1] : null;
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
					var fileName = $"{parsedTime:yyyy-MM-dd-HH-mm-ss} {asset} {counter++}.pdf";
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