using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class AccountsPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h5.card-title:has-text('Account Details')";
    private const string TableSelector = "table.table";
    private const string TableRowSelector = "table.table tbody tr";
	private const string EmptyStateSelector = "h5.text-muted:has-text('No Account Data Found')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string AccountsLinkSelector = "a.dropdown-item:has-text('Account Details')";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForSelectorAsync(AccountsLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.ClickAsync(AccountsLinkSelector);
            await _page.WaitForURLAsync("**/accounts", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
        });
        await WaitForPageLoadAsync(ct: CancellationToken.None);
    }

    public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var targetUrl = relativePath ?? "/accounts";
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

    public async Task<bool> HasAccountDataRowsAsync(int minimumRows = 1)
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
            return rows.Count >= minimumRows;
        }
        catch { return false; }
    }
}
