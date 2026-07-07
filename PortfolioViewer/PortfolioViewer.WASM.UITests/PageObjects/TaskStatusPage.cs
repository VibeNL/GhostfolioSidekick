using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

public class TaskStatusPage(IPage page) : BasePageObject(page)
{
    private const string PageHeadingSelector = "h1:has-text('Task Status')";
    private const string LoadingSpinnerSelector = ".spinner-border";
    private const string ErrorAlertSelector = ".alert-danger";
    private const string TaskStatusLinkSelector = "a.dropdown-item:has-text('Task Status')";
    private const string QuickRefreshButtonSelector = "button:has-text('Quick Refresh')";
    private const string TableSelector = ".table";
    private const string TaskRowsSelector = ".table.table-striped.table-hover.mb-0 tbody tr";
    private const string NoTaskDataMessageSelector = "p:has-text('No task data available. Tasks may not be initialized yet.')";

    public async Task NavigateViaMenuAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.ClickAsync("a.nav-link.dropdown-toggle:has-text('System')");
            await _page.WaitForTimeoutAsync(500);
            await _page.ClickAsync(TaskStatusLinkSelector);
            await _page.WaitForTimeoutAsync(1000);
        });
    }

    public async Task NavigateDirectAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            await _page.GotoAsync("/task-status");
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
                $"{PageHeadingSelector}, {ErrorAlertSelector}, {TableSelector}, {NoTaskDataMessageSelector}",
                new PageWaitForSelectorOptions { Timeout = timeout });
        });
    }

    public async Task<bool> HasTaskStatusTitleAsync()
    {
        try
        {
            var title = await _page.QuerySelectorAsync(PageHeadingSelector);
            return title != null && await title.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task<bool> HasQuickRefreshButtonAsync()
    {
        try
        {
            var btn = await _page.QuerySelectorAsync(QuickRefreshButtonSelector);
            return btn != null && await btn.IsVisibleAsync();
        }
        catch { return false; }
    }

    public async Task QuickRefreshAsync()
    {
        await ExecuteWithErrorCheckAsync(async () =>
        {
            var btn = await _page.QuerySelectorAsync(QuickRefreshButtonSelector);
            if (btn != null)
            {
                await btn.ClickAsync();
                await _page.WaitForTimeoutAsync(1000);
            }
        });
    }

    public async Task<bool> HasTasksListAsync()
    {
        try
        {
            var table = await _page.QuerySelectorAsync(TableSelector);
            return table != null && await table.IsVisibleAsync();
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

    public async Task<bool> HasTaskRowsAsync(int minimumRows = 1)
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(TaskRowsSelector);
            return rows.Count >= minimumRows;
        }
        catch { return false; }
    }

    public async Task<bool> HasNoTaskDataMessageAsync()
    {
        try
        {
            var message = await _page.QuerySelectorAsync(NoTaskDataMessageSelector);
            return message != null && await message.IsVisibleAsync();
        }
        catch { return false; }
    }
}
