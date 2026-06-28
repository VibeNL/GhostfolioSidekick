using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class TablesPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h1:has-text('Table Viewer')";
    private const string LoadingSelector = ".alert:has-text('Loading')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string TablesLinkSelector = "a.dropdown-item:has-text('Data Tables')";
    private const string TableSelectorElement = "#tableSelect";
    private const string TableDataSelector = ".table";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('System')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(TablesLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync()
    {
        // Click the Data Tables nav link to navigate within the SPA (preserves auth)
        // First open the System dropdown if not already open
        await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('System')");
        await _page.WaitForTimeoutAsync(500);
        await _page.ClickAsync("a.dropdown-item:has-text('Data Tables')");
    }

    public async Task WaitForPageLoadAsync(int timeout = 30000)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.WaitForSelectorAsync(
                $"{PageHeadingSelector}, {TableSelectorElement}",
                new PageWaitForSelectorOptions { Timeout = timeout });
        });
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
                await _page.WaitForTimeoutAsync(1000);
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
