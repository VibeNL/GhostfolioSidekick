using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class UpcomingDividendsPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h4.card-title:has-text('Dividends')";
    private const string LoadingSpinnerSelector = ".spinner-border:has-text('Loading Dividends')";
    private const string EmptyStateSelector = "h5.text-muted:has-text('No Dividends')";
    private const string DividendsLinkSelector = "a.dropdown-item:has-text('Dividends')";
    private const string TableSelector = "table.table-hover";

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

    public async Task NavigateDirectAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync("/dividends");
        });
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

            await _page.WaitForSelectorAsync(
                $"{PageHeadingSelector}, {EmptyStateSelector}, {TableSelector}",
                new PageWaitForSelectorOptions { Timeout = timeout });
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
}
