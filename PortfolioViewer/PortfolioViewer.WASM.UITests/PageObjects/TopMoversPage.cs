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

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('Portfolio')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(TopMoversLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync("/top-movers");
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
                $"{PageHeadingSelector}, {ErrorAlertSelector}, {RisersCardSelector}",
                new PageWaitForSelectorOptions { Timeout = timeout });
        });
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
}
