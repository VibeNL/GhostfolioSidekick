using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class TaxReportPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h2:has-text('Tax Report')";
    private const string LoadingSpinnerSelector = ".spinner-border:has-text('Loading Tax Report')";
    private const string EmptyStateSelector = "h5.text-muted:has-text('No Tax Data Found')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string TaxReportLinkSelector = "a.dropdown-item:has-text('Tax Report')";
    private const string TableButtonSelector = "button:has-text('Table')";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(TaxReportLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync("/tax-report");
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
                $"{PageHeadingSelector}, {EmptyStateSelector}, {ErrorAlertSelector}",
                new PageWaitForSelectorOptions { Timeout = timeout });
        });
    }

    public async Task<bool> HasTaxReportTitleAsync()
    {
        try
        {
            var title = await _page.QuerySelectorAsync(PageHeadingSelector);
            return title != null && await title.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasYearSelectorAsync()
    {
        try
        {
            var yearButtons = await _page.QuerySelectorAllAsync("button:has-text('All')");
            return yearButtons.Count > 0;
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
}
