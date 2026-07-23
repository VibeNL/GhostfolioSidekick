using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class PortfolioTimeSeriesPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = ".card-header .card-title:has-text('Portfolio Time Series')";
	private const string EmptyStateSelector = "h5.text-muted:has-text('No time series data found')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string TimeSeriesLinkSelector = "a.dropdown-item:has-text('Performance Timeline')";
    private const string ChartButtonSelector = "button:has-text('Chart')";
    private const string TableButtonSelector = "button:has-text('Table')";
    private const string TableRowSelector = "table.table tbody tr";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForSelectorAsync(TimeSeriesLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.ClickAsync(TimeSeriesLinkSelector);
            await _page.WaitForURLAsync("**/portfolio-timeseries", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
        });
        await WaitForPageLoadAsync(ct: CancellationToken.None);
    }

    public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var targetUrl = relativePath ?? "/portfolio-timeseries";
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
        await base.WaitForPageLoadAsync([PageHeadingSelector, EmptyStateSelector, ErrorAlertSelector, ".alert-danger"], timeout, ct);
    }

    public async Task<bool> HasTimeSeriesTitleAsync()
    {
        try
        {
            var title = await _page.QuerySelectorAsync(PageHeadingSelector);
            return title != null && await title.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasViewModeToggleAsync()
    {
        try
        {
            var chart = await _page.QuerySelectorAsync(ChartButtonSelector);
            var table = await _page.QuerySelectorAsync(TableButtonSelector);
            return chart != null && table != null;
        }
        catch { return false; }
    }

    public async Task SwitchToTableModeAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var tableBtn = await _page.QuerySelectorAsync(TableButtonSelector);
            if (tableBtn != null)
            {
                await tableBtn.ClickAsync();
                await _page.WaitForSelectorAsync(TableRowSelector, new PageWaitForSelectorOptions { Timeout = 5000 });
            }
        });
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

    public async Task<bool> HasTimeSeriesRowsAsync(int minimumRows = 1)
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
            return rows.Count >= minimumRows;
        }
        catch { return false; }
    }
}
