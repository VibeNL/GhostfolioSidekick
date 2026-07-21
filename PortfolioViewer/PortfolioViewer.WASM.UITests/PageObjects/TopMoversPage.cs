using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class TopMoversPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h2:has-text('Top 3 Risers')";
    private const string LoadingSpinnerSelector = ".spinner-border:has-text('Loading Top Movers')";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string TopMoversLinkSelector = "a.dropdown-item:has-text('Top Movers')";
    private const string RisersCardSelector = ".card.border-success";
    private const string LosersCardSelector = ".card.border-danger";
    private const string RiserEntriesSelector = ".card.border-success .card-body .d-flex.justify-content-between.align-items-center";
    private const string LoserEntriesSelector = ".card.border-danger .card-body .d-flex.justify-content-between.align-items-center";
    private const string NoPositivePerformersSelector = "p:has-text('No positive performers found in this time range')";
    private const string NoLosingPerformersSelector = "p:has-text('No losing positions found in this time range')";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForSelectorAsync(TopMoversLinkSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await _page.ClickAsync(TopMoversLinkSelector);
            await _page.WaitForURLAsync("**/top-movers", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
        });
    }

    public async Task NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var targetUrl = relativePath ?? "/top-movers";
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
        await base.WaitForPageLoadAsync([PageHeadingSelector, ErrorAlertSelector, RisersCardSelector, ".alert-danger"], timeout, ct);
    }

    public async Task<bool> HasTopMoversTitleAsync()
    {
        try
        {
            var title = await _page.QuerySelectorAsync(PageHeadingSelector);
            return title != null && await title.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasRisersSectionAsync()
    {
        try
        {
            var risers = await _page.QuerySelectorAsync(RisersCardSelector);
            return risers != null && await risers.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasLosersSectionAsync()
    {
        try
        {
            var losers = await _page.QuerySelectorAsync(LosersCardSelector);
            return losers != null && await losers.IsVisibleAsync();
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

    public async Task<bool> HasRiserEntriesAsync(int minimumRows = 1)
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(RiserEntriesSelector);
            return rows.Count >= minimumRows;
        }
        catch { return false; }
    }

    public async Task<bool> HasLoserEntriesAsync(int minimumRows = 1)
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(LoserEntriesSelector);
            return rows.Count >= minimumRows;
        }
        catch { return false; }
    }

    public async Task<bool> HasNoRisersMessageAsync()
    {
        try
        {
            var message = await _page.QuerySelectorAsync(NoPositivePerformersSelector);
            return message != null && await message.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasNoLosersMessageAsync()
    {
        try
        {
            var message = await _page.QuerySelectorAsync(NoLosingPerformersSelector);
            return message != null && await message.IsVisibleAsync();
        }
        catch { return false; }
    }
}
