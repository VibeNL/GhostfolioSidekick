using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class HoldingsPriceTargetsPage(IPage page) : BasePageObject(page)
{
	private const string PageHeadingSelector = "h5.card-title:has-text('Holdings vs. Price Targets')";
	private const string TableRowSelector = "table.table tbody tr";
	private const string EmptyStateSelector = "h5.text-muted:has-text('No Matching Holdings Found')";
	private const string ErrorAlertSelector = ".alert-danger";
	private const string HoldingsPriceTargetsLinkSelector = "a.dropdown-item[href='holdings-price-targets']";

	public async Task NavigateViaMenuAsync()
	{
		await ExecuteWithErrorCheckAsync(async () =>
		{
			await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
			await _page.WaitForSelectorAsync(HoldingsPriceTargetsLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
			await _page.ClickAsync(HoldingsPriceTargetsLinkSelector);
			await _page.WaitForURLAsync("**/holdings-price-targets", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
		});
		await WaitForPageLoadAsync(ct: CancellationToken.None);
	}

	public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
	{
		await ExecuteWithErrorCheckAsync(async () =>
		{
			var targetUrl = relativePath ?? "/holdings-price-targets";
			if (!Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
			{
				var baseUri = new Uri(_page.Url);
				targetUrl = new Uri(baseUri, targetUrl).ToString();
			}
			await _page.GotoAsync(targetUrl);
		}, ct);
		await WaitForPageLoadAsync(ct: ct);
	}

	public async Task WaitForPageLoadAsync(int timeout = 30000, CancellationToken ct = default)
	{
		await base.WaitForPageLoadAsync(
			["h5:has-text('Holdings vs. Price Targets')"],
			timeout,
			ct);
	}

	public async Task<bool> HasHoldingsPriceTargetsHeadingAsync()
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

	public async Task<bool> HasDataRowsAsync(int minimumRows = 1)
	{
		try
		{
			var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
			return rows.Count >= minimumRows;
		}
		catch { return false; }
	}

	public async Task<bool> HasSymbolAsync(string symbol)
	{
		try
		{
			var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
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

	public async Task<List<string>> GetSymbolsAsync()
	{
		var symbols = new List<string>();
		try
		{
			var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
			foreach (var row in rows)
			{
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
