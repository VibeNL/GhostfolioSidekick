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
			await _page.WaitForSelectorAsync(PriceTargetsLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
			await _page.ClickAsync(PriceTargetsLinkSelector);
			await _page.WaitForURLAsync("**/price-targets", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
		});
	}

	public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
	{
		await ExecuteWithErrorCheckAsync(async () =>
		{
			var targetUrl = relativePath ?? "/price-targets";
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
		await base.WaitForPageLoadAsync(
			["h5:has-text('Analyst Price Targets')"],
			timeout,
			ct);
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
