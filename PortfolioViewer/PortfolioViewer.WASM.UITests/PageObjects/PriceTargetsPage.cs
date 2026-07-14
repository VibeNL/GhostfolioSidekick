using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class PriceTargetsPage(IPage page) : BasePageObject(page)
{
	private const string PageHeadingSelector = "h5.card-title:has-text('Analyst Price Targets')";
	private const string TableSelector = "table.table";
	private const string TableRowSelector = "table.table tbody tr";
	private const string LoadingSpinnerSelector = ".spinner-border";
	private const string EmptyStateSelector = "h5.text-muted:has-text('No Price Targets Found')";
	private const string ErrorAlertSelector = ".alert-danger";
	private const string PriceTargetsLinkSelector = "a.dropdown-item:has-text('Price Targets')";

	public async Task NavigateViaMenuAsync()
	{
		await ExecuteWithErrorCheckAsync(async () =>
		{
			await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
			await _page.WaitForTimeoutAsync(500);
			await _page.ClickAsync(PriceTargetsLinkSelector);
			await _page.WaitForTimeoutAsync(1000);
		});
	}

	public async Task NavigateDirectAsync(string? serverBaseUrl = null)
	{
		await ExecuteWithErrorCheckAsync(async () =>
		{
			var url = serverBaseUrl ?? _page.Url;
			if (string.IsNullOrEmpty(url))
			{
				throw new InvalidOperationException("Cannot navigate: no base URL available");
			}
			var baseUri = new Uri(url);
			var targetUrl = new Uri(baseUri, "/price-targets").ToString();
			await _page.GotoAsync(targetUrl);
		});
	}

	public async Task WaitForPageLoadAsync(int timeout = 30000)
	{
		try
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				// Wait for the page to reach a stable state (not loading).
				// First wait for loading to complete, then wait for content.
				var shortTimeout = Math.Min(5000, timeout / 5);
				var contentTimeout = Math.Max(10000, timeout / 2);

				// Wait for loading spinner to disappear (or timeout quickly)
				try
				{
					await _page.WaitForSelectorAsync(".spinner-border", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden, Timeout = shortTimeout });
				}
				catch
				{
					// No spinner or already hidden - continue
				}

				// Wait for any stable state: heading, empty state, error, or content
				var selectors = new[]
				{
					"h5:has-text('Analyst Price Targets')",
					"h4:has-text('Error Loading Data')",
					"h5:has-text('No Price Targets Found')",
					".alert-danger",
					".card-body"
				};
				foreach (var selector in selectors)
				{
					try
					{
						await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = contentTimeout });
						return; // Found a stable state
					}
					catch
					{
						// Try next selector
					}
				}
			});
		}
		catch
		{
			// Navigation or wait may fail; that's acceptable in test env
		}
	}

	public async Task<bool> HasPriceTargetsHeadingAsync()
	{
		try
		{
			var element = await _page.QuerySelectorAsync(PageHeadingSelector);
			return element != null && await element.IsVisibleAsync();
		}
		catch { return false; }
	}

	public async Task<bool> IsEmptyStateDisplayedAsync()
	{
		try
		{
			var element = await _page.QuerySelectorAsync(EmptyStateSelector);
			return element != null && await element.IsVisibleAsync();
		}
		catch { return false; }
	}

	public async Task<bool> IsErrorDisplayedAsync()
	{
		try
		{
			var element = await _page.QuerySelectorAsync(ErrorAlertSelector);
			return element != null && await element.IsVisibleAsync();
		}
		catch { return false; }
	}

	public async Task<bool> HasPriceTargetDataRowsAsync(int minimumRows = 1)
	{
		try
		{
			var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
			return rows.Count >= minimumRows;
		}
		catch { return false; }
	}

	public async Task<bool> HasPriceTargetSymbolAsync(string symbol)
	{
		try
		{
			var rows = await _page.QuerySelectorAllAsync("table.table tbody tr");
			foreach (var row in rows)
			{
				var text = await row.TextContentAsync() ?? "";
				if (text.Contains(symbol, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
		catch { return false; }
	}

	public async Task<List<string>> GetPriceTargetSymbolsAsync()
	{
		var symbols = new List<string>();
		try
		{
			var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
			foreach (var row in rows)
			{
				var text = await row.TextContentAsync() ?? "";
				var cells = await row.QuerySelectorAllAsync("td");
				if (cells.Count > 0)
				{
					var cellText = await cells[0].TextContentAsync() ?? "";
					symbols.Add(cellText);
				}
			}
		}
		catch { }
		return symbols;
	}
}
