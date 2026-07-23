using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class DataIssuesPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h5.text-success:has-text('No Data Issues Found')";
	private const string ErrorAlertSelector = ".alert-danger";
    private const string DataIssuesLinkSelector = "a.dropdown-item:has-text('Data Issues')";
    private const string IssuesListSelector = ".list-group";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('System')");
            await _page.WaitForSelectorAsync(DataIssuesLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.ClickAsync(DataIssuesLinkSelector);
            await _page.WaitForURLAsync("**/data-issues", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
        });
        await WaitForPageLoadAsync(ct: CancellationToken.None);
    }

    public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var targetUrl = relativePath ?? "/data-issues";
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
        await base.WaitForPageLoadAsync(["h5:has-text('No Data Issues Found')", ErrorAlertSelector, ".list-group", ".card"], timeout, ct);
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
