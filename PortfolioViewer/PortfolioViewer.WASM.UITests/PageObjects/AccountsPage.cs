using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class AccountsPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h5.card-title:has-text('Account Details')";
    private const string TableSelector = "table.table";
    private const string LoadingSpinnerSelector = ".spinner-border:has-text('Loading Account Data')";
    private const string EmptyStateSelector = "h5.text-muted:has-text('No Account Data Found')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string AccountsLinkSelector = "a.dropdown-item:has-text('Account Details')";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(AccountsLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync("/accounts");
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

    public async Task<bool> HasAccountDataAsync()
    {
        try
        {
            var table = await _page.QuerySelectorAsync(TableSelector);
            return table != null && await table.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasAccountNameAsync(string accountName)
    {
        try
        {
            var element = await _page.QuerySelectorAsync($".table tbody tr td:has-text('{accountName}')");
            return element != null;
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
}
