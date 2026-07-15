using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects
{
	public class TransactionsPage(IPage page) : BasePageObject(page)
	{
		private const string PageHeadingSelector = "h5.card-title:has-text('Transactions')";
		private const string TableSelector = "table.table";
		private const string LoadingSpinnerSelector = ".spinner-border:has-text('Loading Transaction Data')";
		private const string EmptyStateSelector = "h5.text-muted:has-text('No Transactions Found')";
		private const string ErrorAlertSelector = ".alert-danger";
		private const string TransactionRowSelector = "tbody tr";
		private const string TransactionsLinkSelector = "a.dropdown-item:has-text('Transaction History')";
		private const string DateFilterAllButtonSelector = "button.btn:has-text('All')";
		private const string DateFilterApplyButtonSelector = "button.btn:has-text('Apply')";

		public async Task NavigateViaMenuAsync()
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				// Click the Transactions dropdown
				await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Transactions')");
					await _page.WaitForSelectorAsync(TransactionsLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

				// Click the Transaction History link
				await _page.ClickAsync(TransactionsLinkSelector);
					// Wait for SPA navigation to complete
					await _page.WaitForURLAsync("**/transactions", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
			});
		}

		public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				var targetUrl = relativePath ?? "/transactions";
				if (!Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
				{
					var baseUri = new Uri(_page.Url);
					targetUrl = new Uri(baseUri, targetUrl).ToString();
				}
				await _page.GotoAsync(targetUrl);
			}, ct);
		}

		public async Task WaitForPageLoadAsync(int timeout = 30000, CancellationToken ct = default)
		{
			await base.WaitForPageLoadAsync([PageHeadingSelector, EmptyStateSelector, ErrorAlertSelector, ".alert-danger"], timeout, ct);
		}

		public async Task<bool> HasTransactionsAsync()
		{
			try
			{
				var tableExists = await _page.QuerySelectorAsync(TableSelector);
				if (tableExists == null) return false;

				var rows = await _page.QuerySelectorAllAsync(TransactionRowSelector);
				return rows.Count > 0;
			}
			catch
			{
				return false;
			}
		}

		public async Task<int> GetTransactionCountAsync()
		{
			try
			{
				var rows = await _page.QuerySelectorAllAsync(TransactionRowSelector);
				return rows.Count;
			}
			catch
			{
				return 0;
			}
		}

		public async Task<bool> IsEmptyStateDisplayedAsync()
		{
			try
			{
				var element = await _page.QuerySelectorAsync(EmptyStateSelector);
				return element != null && await element.IsVisibleAsync();
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> IsErrorDisplayedAsync()
		{
			try
			{
				var element = await _page.QuerySelectorAsync(ErrorAlertSelector);
				return element != null && await element.IsVisibleAsync();
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> IsTableDisplayedAsync()
		{
			try
			{
				var table = await _page.QuerySelectorAsync(TableSelector);
				return table != null && await table.IsVisibleAsync();
			}
			catch
			{
				return false;
			}
		}

		public async Task<string> GetTotalRecordsTextAsync()
		{
			try
			{
				var heading = await _page.QuerySelectorAsync(PageHeadingSelector);
				return heading != null ? await heading.TextContentAsync() ?? string.Empty : string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		public async Task<List<TransactionRowData>> GetTransactionRowsAsync(int maxRows = 10)
		{
			var transactions = new List<TransactionRowData>();

			try
			{
				var rows = await _page.QuerySelectorAllAsync(TransactionRowSelector);
				var count = Math.Min(rows.Count, maxRows);

				for (int i = 0; i < count; i++)
				{
					var row = rows[i];
					var cells = await row.QuerySelectorAllAsync("td");

					if (cells.Count >= 6)
					{
						transactions.Add(new TransactionRowData
						{
							Date = await cells[0].TextContentAsync() ?? string.Empty,
							Type = await cells[1].TextContentAsync() ?? string.Empty,
							Symbol = await cells[2].TextContentAsync() ?? string.Empty,
							Name = await cells[3].TextContentAsync() ?? string.Empty,
							Account = await cells[4].TextContentAsync() ?? string.Empty,
							TotalValue = await cells[5].TextContentAsync() ?? string.Empty
						});
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading transaction rows: {ex.Message}");
			}

			return transactions;
		}

		public async Task<bool> VerifyTransactionDataAsync()
		{
			// Verify that at least one transaction has non-empty data
			var transactions = await GetTransactionRowsAsync(5);

			if (transactions.Count == 0)
				return false;

			// Check first transaction has reasonable data
			var firstTransaction = transactions[0];
			return !string.IsNullOrWhiteSpace(firstTransaction.Date) &&
				   !string.IsNullOrWhiteSpace(firstTransaction.Type) &&
				   !string.IsNullOrWhiteSpace(firstTransaction.Symbol);
		}

		public async Task<string> TakeScreenshotAsync(string path)
		{
			await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
			return path;
		}

	public async Task SetDateFilterToAllAsync()
	{
		// The Transactions page may not have explicit date filter buttons.
		// If they exist, click them; otherwise the page already shows all data by default.
		try
		{
			var allButton = await _page.QuerySelectorAsync(DateFilterAllButtonSelector);
			if (allButton != null)
			{
				await allButton.ClickAsync();
			}

			var applyButton = await _page.QuerySelectorAsync(DateFilterApplyButtonSelector);
			if (applyButton != null)
			{
				await applyButton.ClickAsync();
			}

			// Wait for the filter to apply and data to reload
			await _page.WaitForSelectorAsync(TableSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
		}
		catch
		{
			// Date filter buttons may not exist on this page - that's acceptable
		}
	}
	}

	public class TransactionRowData
	{
		public string Date { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public string Symbol { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Account { get; set; } = string.Empty;
		public string TotalValue { get; set; } = string.Empty;

		public override string ToString()
		{
			return $"[{Date}] {Type} - {Symbol} ({Name}) - {TotalValue}";
		}
	}
}
