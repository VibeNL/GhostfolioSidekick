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
    private const string TableRowSelector = "table.table tbody tr";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForSelectorAsync(TaxReportLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.ClickAsync(TaxReportLinkSelector);
            await _page.WaitForURLAsync("**/tax-report", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
        });
    }

    public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var targetUrl = relativePath ?? "/tax-report";
            if (!Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
            {
                var baseUri = new Uri(_page.Url);
                targetUrl = new Uri(baseUri, targetUrl).ToString();
            }
            await _page.GotoAsync(targetUrl);
        }, ct);
    }

    public async Task WaitForPageLoadAsync(int timeout = 30000, CancellationToken ct = default)
    {
        await base.WaitForPageLoadAsync([PageHeadingSelector, EmptyStateSelector, ErrorAlertSelector, ".alert-danger"], timeout, ct);
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
                await _page.WaitForSelectorAsync(TableRowSelector, new PageWaitForSelectorOptions { Timeout = 5000 });
            }
        });
    }

    public async Task<bool> HasReportRowsAsync(int minimumRows = 1)
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
            return rows.Count >= minimumRows;
        }
        catch { return false; }
    }

    public async Task<bool> HasAccountNameAsync(string accountName)
    {
        try
        {
            var accountLink = await _page.QuerySelectorAsync($"table.table tbody tr td a:has-text('{accountName}')");
            return accountLink != null && await accountLink.IsVisibleAsync();
        }
        catch { return false; }
    }
}
