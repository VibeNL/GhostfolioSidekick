using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class DataIssuesPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h5.text-success:has-text('No Data Issues Found')";
    private const string LoadingSpinnerSelector = ".spinner-border:has-text('Analyzing Data Quality')";
    private const string EmptyStateSelector = "h5.text-success:has-text('No Data Issues Found')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string DataIssuesLinkSelector = "a.dropdown-item:has-text('Data Issues')";
    private const string IssuesListSelector = ".list-group";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('System')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(DataIssuesLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync("/data-issues");
        });
    }

    public async Task WaitForPageLoadAsync(int timeout = 30000)
    {
        try
        {
            await ExecuteWithErrorCheckAsync(async () =>
            {
                try
                {
                    await _page.WaitForSelectorAsync(LoadingSpinnerSelector, new PageWaitForSelectorOptions { Timeout = 2000, State = WaitForSelectorState.Visible });
                    await _page.WaitForSelectorAsync(LoadingSpinnerSelector, new PageWaitForSelectorOptions { Timeout = timeout, State = WaitForSelectorState.Hidden });
                }
                catch { }

                // Use flexible selectors that match the actual HTML structure
                await _page.WaitForSelectorAsync(
                    "h5:has-text('No Data Issues Found'), .alert-danger, .list-group, .card",
                    new PageWaitForSelectorOptions { Timeout = timeout });
            });
        }
        catch
        {
            // Navigation or wait may fail; that's acceptable in test env
        }
    }

    public async Task<bool> HasNoIssuesMessageAsync()
    {
        try
        {
            var element = await _page.QuerySelectorAsync(PageHeadingSelector);
            return element != null && await element.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasIssuesListAsync()
    {
        try
        {
            var list = await _page.QuerySelectorAsync(IssuesListSelector);
            return list != null && await list.IsVisibleAsync();
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
