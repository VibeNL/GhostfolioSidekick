using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	internal abstract partial class TransactionPage(IPage page, ILogger logger)
	{
		protected const string Description = "Transaction reference";

		private const string Url = "https://de.scalable.capital/cockpit/";

		// Exposed as properties so derived classes can use them without re-capturing the primary constructor parameters (avoids CS9107/CS9124)
		protected IPage Page { get; } = page;
		protected ILogger Logger { get; } = logger;

		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			Logger.LogInformation("Scraping transactions...");

			await SetupFilters();
			await ScrollDown(Page);

			var list = new List<ActivityWithSymbol>();
			int counter = 0;
			foreach (var transaction in await GetTransactions())
			{
				for (int i = 0; i < 3; i++)
				{
					try
					{
						Logger.LogInformation("Processing transaction {Counter}...", counter);

						// Open the transaction detail panel
						await OpenTransactionDetail(transaction);

						// Process transaction details
						var generatedTransaction = await ProcessDetails(transaction);
						if (generatedTransaction != null)
						{
							var symbol = await AddSymbol(generatedTransaction);
							if (symbol != null)
							{
								list.Add(symbol);
							}

							Logger.LogInformation("Transaction {Counter} processed. Generated {GeneratedTransaction}", counter, generatedTransaction.ToString());
						}

						// Close the transaction detail panel
						await CloseTransactionDetail();

						break;
					}
					catch (Exception ex)
					{
						// Try to close the detail panel if open
						try
						{
							await CloseTransactionDetail();
						}
						catch
						{
							// Ignore
						}

						Logger.LogError(ex, "Error processing transaction {Counter}: {Message}", counter, ex.Message);
					}
				}

				counter++;
			}

			return list;
		}

		protected virtual Task SetupFilters() => Task.CompletedTask;

		protected virtual Task OpenTransactionDetail(ILocator transaction) => Task.CompletedTask;

		protected virtual Task CloseTransactionDetail() => Task.CompletedTask;

		protected abstract Task<Activity?> ProcessDetails(ILocator transaction);

		private async Task ScrollDown(IPage page)
		{
			Logger.LogInformation("Scrolling down to load all transactions...");

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

			Logger.LogInformation("All transactions loaded.");
		}

		protected virtual async Task<ActivityWithSymbol?> AddSymbol(Activity? generatedTransaction)
		{
			if (generatedTransaction is null)
			{
				return null;
			}

			if (generatedTransaction is CashDepositActivity || generatedTransaction is CashWithdrawalActivity || generatedTransaction is InterestActivity)
			{
				return new ActivityWithSymbol
				{
					Activity = generatedTransaction,
					Symbol = default!,
				};
			}

			var link = Page.Locator("[href*=\"/broker/security?\"]").Last;
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
			return Page.GetByTestId(TransactionTestId()).AllAsync();
		}

		private Task<int> GetTransacionsCount()
		{
			// Count number of divs with role list
			return Page.GetByTestId(TransactionTestId()).CountAsync();
		}

		protected async Task<DateTime> GetHistoryDate(string description)
		{
			try
			{
				// find the div with the first child containing the text History
				var historyNode = Page.Locator("div").GetByText("History").First;
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

		protected async Task<Money> GetMoneyField(string description)
		{
			return new Money(Currency.EUR, await GetField<decimal>(description));
		}

		protected async Task<T> GetField<T>(string description)
		{
			var container = Page
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
			await Page.GotoAsync(Url);
		}

		[GeneratedRegex(".*transaction.*")]
		private static partial Regex TransactionTestId();
	}
}
