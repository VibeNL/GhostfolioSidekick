using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class DividendsPage(IPage page) : BasePageObject(page)
{
	private const string PageHeadingSelector = "h4:has-text('Dividends')";
	private const string ErrorAlertSelector = ".alert-danger";
	private const string TableRowSelector = "table.table tbody tr";

	public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
	{
		await ExecuteWithErrorCheckAsync(async () =>
		{
			var targetUrl = relativePath ?? "/dividends";
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
		await base.WaitForPageLoadAsync([PageHeadingSelector, ErrorAlertSelector, ".alert-danger", "table.table"], timeout, ct);
	}

	public async Task<bool> HasDividendsDataRowsAsync(int minimumRows = 1)
	{
		try
		{
			var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
			return rows.Count >= minimumRows;
		}
		catch { return false; }
	}
}
