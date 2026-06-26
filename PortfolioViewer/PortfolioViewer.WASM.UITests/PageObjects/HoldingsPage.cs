using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class HoldingsPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h5.card-title:has-text('Portfolio Overview')";
    private const string TableSelector = "table.table";
    private const string LoadingSpinnerSelector = ".spinner-border";
    private const string EmptyStateSelector = "h5.text-muted:has-text('No Holdings Found')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string HoldingsLinkSelector = "a.dropdown-item:has-text('Holdings Overview')";
    private const string TreemapButtonSelector = "button:has-text('Treemap')";
    private const string TableButtonSelector = "button:has-text('Table')";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(HoldingsLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync("/holdings");
        });
    }

    public async Task WaitForPageLoadAsync(CancellationToken ct = default, int timeout = 30000)
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
                $"{PageHeadingSelector}, {EmptyStateSelector}, {ErrorAlertSelector}",
                new PageWaitForSelectorOptions { Timeout = timeout });
        });
    }

    public async Task<bool> HasPortfolioOverviewAsync()
    {
        try
        {
            var element = await _page.QuerySelectorAsync(PageHeadingSelector);
            return element != null && await element.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasViewModeToggleAsync()
    {
        try
        {
            var treemap = await _page.QuerySelectorAsync(TreemapButtonSelector);
            var table = await _page.QuerySelectorAsync(TableButtonSelector);
            return treemap != null && table != null;
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
                await _page.WaitForTimeoutAsync(500);
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
}
