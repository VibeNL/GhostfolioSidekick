using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace ScraperUtilities.CentraalBeheer
{
	internal partial class TransactionPage(IPage page, Microsoft.Extensions.Logging.ILogger logger)
	{
		readonly CultureInfo cultureInfo = new("nl-NL");
		private const string Prefix = "Centraal Beheer ";

		internal async Task<IEnumerable<ActivityWithSymbol>> ScrapeTransactions()
		{
			logger.LogInformation("Scraping transactions...");

			await SetDateSelection(page);
			await ScrollDown(page);

			var list = new List<ActivityWithSymbol>();
			int counter = 0;
			foreach (var transaction in await GetTransactions())
			{
				logger.LogInformation($"Processing transaction {counter}...");

				// Click on the transaction to open the details
				await transaction.ScrollIntoViewIfNeededAsync();

				// Process transaction details
				var generatedTransaction = await ProcessDetails(counter);
				if (generatedTransaction == null)
				{
					counter++;
					continue;
				}

				var symbol = await AddSymbol(counter, generatedTransaction);
				if (symbol != null)
				{
					list.Add(symbol);
				}

				logger.LogInformation("Transaction {Counter} processed. Generated {GeneratedTransaction}", counter, generatedTransaction.ToString());

				counter++;
			}

			return list;
		}

		private async Task ScrollDown(IPage page)
		{
			logger.LogInformation("Scrolling down to load all transactions...");

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

			logger.LogInformation("All transactions loaded.");
		}

		private async Task SetDateSelection(IPage page)
		{
			logger.LogInformation("Setting date to all dates...");
			await page.Locator("#filter-van").FillAsync("01-01-2010");
			await page.Locator("#filter-tot").FillAsync(DateTime.Now.ToString("dd-MM-yyyy"));

			Thread.Sleep(1000);
		}

		private async Task<ActivityWithSymbol?> AddSymbol(int counter, Activity? generatedTransaction)
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

			var header = page.Locator($"#transactie-title-{counter}");
			var symbol = Prefix + await header.Locator("span").InnerTextAsync();

			return new ActivityWithSymbol
			{
				Activity = generatedTransaction,
				Symbol = symbol,
				symbolName = symbol
			};
		}

		private Task<IReadOnlyList<ILocator>> GetTransactions()
		{
			return page.Locator("div[id^='transactie-collapsable-']").AllAsync();
		}

		private Task<int> GetTransacionsCount()
		{
			// Count number of divs with role list
			return page.Locator("div[id^='transactie-collapsable-']").CountAsync();
		}

		private async Task<Activity?> ProcessDetails(int counter)
		{
			var header = page.Locator($"#transactie-title-{counter}");
			var type = await header.Locator("strong").InnerTextAsync();

			switch (type)
			{
				case "Overboeking":
					return new CashDepositWithdrawalActivity
					{
						Amount = await GetMoneyField($"#brutoBedrag-{counter}"),
						Date = await GetDateField($"#verwerkingsdatum-{counter}"),
						TransactionId = Guid.NewGuid().ToString(),
					};

				case "Aankoop":
				case "Verkoop":

					var isSell = type == "Verkoop";
					BuySellActivityFee[] fees = [];

					if (await HasField($"#aankoopkosten-{counter}"))
					{
						fees = [new BuySellActivityFee(await GetMoneyField($"#aankoopkosten-{counter}"))];
					}

					return new BuySellActivity
					{
						Quantity = (isSell ? -1 : 1) * await GetField<decimal>($"#participaties-{counter}"),
						UnitPrice = await GetMoneyField($"#koers-{counter}"),
						TotalTransactionAmount = await GetMoneyField($"#brutoBedrag-{counter}"),
						Fees = [],
						Date = await GetDateField($"#verwerkingsdatum-{counter}"),
						TransactionId = Guid.NewGuid().ToString(),
					};

				default:
					break;
			}

			return null;
		}

		private async Task<Money> GetMoneyField(string fieldId)
		{
			return new Money(Currency.EUR, await GetField<decimal>(fieldId));
		}

		private async Task<DateTime> GetDateField(string fieldId)
		{
			return DateTime.ParseExact(await GetField<string>(fieldId), "d MMMM yyyy", cultureInfo);
		}

		private async Task<T> GetField<T>(string fieldId)
		{
			var text = await page
					.Locator(fieldId)
					.TextContentAsync();

			text = text.Trim();

			if (typeof(T) == typeof(decimal))
			{
				text = text.Replace("€", "").Trim();
				return (T)Convert.ChangeType(decimal.Parse(text, NumberStyles.Currency, cultureInfo), typeof(T));
			}

			return (T)Convert.ChangeType(text, typeof(T));
		}

		private async Task<bool> HasField(string fieldId)
		{
			return (await page.Locator(fieldId).CountAsync()) != 0;
		}
	}
}