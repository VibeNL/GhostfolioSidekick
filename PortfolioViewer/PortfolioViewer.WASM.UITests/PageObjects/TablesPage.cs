using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class TablesPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h1:has-text('Table Viewer')";
	private const string ErrorAlertSelector = ".alert-danger";
    private const string TablesLinkSelector = "a.dropdown-item:has-text('Data Tables')";
    private const string TableSelectorElement = "#tableSelect";
    private const string TableDataSelector = ".table";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('System')");
            await _page.WaitForSelectorAsync(TablesLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.ClickAsync(TablesLinkSelector);
            await _page.WaitForURLAsync("**/tables", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
        });
        await WaitForPageLoadAsync(ct: CancellationToken.None);
    }

    public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            // Click the Data Tables nav link to navigate within the SPA (preserves auth)
            // First open the System dropdown if not already open
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('System')");
            await _page.WaitForSelectorAsync("a.dropdown-item:has-text('Data Tables')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.ClickAsync("a.dropdown-item:has-text('Data Tables')");
        }, ct);
        await WaitForPageLoadAsync(ct: ct);
    }

    public async Task WaitForPageLoadAsync(int timeout = 30000, CancellationToken ct = default)
    {
        await base.WaitForPageLoadAsync([PageHeadingSelector, TableSelectorElement, ".alert-danger"], timeout, ct);
    }

    public async Task<bool> HasTableViewerTitleAsync()
    {
        try
        {
            var title = await _page.QuerySelectorAsync(PageHeadingSelector);
            return title != null && await title.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasTableSelectorAsync()
    {
        try
        {
            var selector = await _page.QuerySelectorAsync(TableSelectorElement);
            return selector != null && await selector.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task SelectTableAsync(string tableName)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var selector = await _page.QuerySelectorAsync(TableSelectorElement);
            if (selector != null)
            {
                await selector.SelectOptionAsync(tableName);
                await _page.WaitForSelectorAsync(TableDataSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
            }
        });
    }

    public async Task<bool> HasDataLoadedAsync()
    {
        try
        {
            var table = await _page.QuerySelectorAsync(TableDataSelector);
            return table != null && await table.IsVisibleAsync();
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
}
