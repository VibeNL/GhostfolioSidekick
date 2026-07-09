using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class UpcomingDividendsPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h4.card-title:has-text('Dividends')";
    private const string LoadingSpinnerSelector = ".spinner-border:has-text('Loading Dividends')";
    private const string EmptyStateSelector = "h5.text-muted:has-text('No Dividends')";
    private const string DividendsLinkSelector = "a.dropdown-item:has-text('Dividends')";
    private const string TableSelector = "table.table-hover";
    private const string TableRowSelector = "table.table-hover tbody tr";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Transactions')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(DividendsLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync(string serverAddress, CancellationToken ct = default)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync(serverAddress + "/dividends");
        }, ct);
    }

    public async Task WaitForPageLoadAsync(int timeout = 30000)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            try
            {
                await _page.WaitForSelectorAsync(LoadingSpinnerSelector, new PageWaitForSelectorOptions { Timeout = 2000, State = WaitForSelectorState.Visible });
                await _page.WaitForSelectorAsync(LoadingSpinnerSelector, new PageWaitForSelectorOptions { Timeout = timeout, State = WaitForSelectorState.Hidden });
            }
            catch { }

            // Wait for any of the page elements, with a short retry loop
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var el = await _page.QuerySelectorAsync(PageHeadingSelector);
                    if (el != null && await el.IsVisibleAsync()) return;
                }
                catch { }
                try
                {
                    var el = await _page.QuerySelectorAsync(EmptyStateSelector);
                    if (el != null && await el.IsVisibleAsync()) return;
                }
                catch { }
                try
                {
                    var el = await _page.QuerySelectorAsync(TableSelector);
                    if (el != null && await el.IsVisibleAsync()) return;
                }
                catch { }
                await _page.WaitForTimeoutAsync(200);
            }
        });
    }

    public async Task<bool> HasDividendsTitleAsync()
    {
        try
        {
            var title = await _page.QuerySelectorAsync(PageHeadingSelector);
            return title != null && await title.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasDividendTableAsync()
    {
        try
        {
            var table = await _page.QuerySelectorAsync(TableSelector);
            return table != null && await table.IsVisibleAsync();
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

    public async Task<bool> HasDividendRowsAsync(int minimumRows = 1)
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
            return rows.Count >= minimumRows;
        }
        catch { return false; }
    }

    public async Task<bool> HasDividendSymbolAsync(string symbol)
    {
        try
        {
            var symbolCell = await _page.QuerySelectorAsync($"table.table-hover tbody tr td strong:has-text('{symbol}')");
            return symbolCell != null && await symbolCell.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> IsErrorDisplayedAsync()
    {
        try
        {
            var errorElement = await _page.QuerySelectorAsync("#blazor-error-ui");
            if (errorElement != null && await errorElement.IsVisibleAsync())
            {
                var errorText = await errorElement.TextContentAsync() ?? string.Empty;
                Console.WriteLine($"Blazor error detected on Dividends page: {errorText}");
                return true;
            }
            return false;
        }
        catch { return false; }
    }
}
