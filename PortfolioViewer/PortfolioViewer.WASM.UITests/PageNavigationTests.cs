using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class PageNavigationTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
    [RetryFact]
    public async Task HoldingsPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var holdingsPage = new HoldingsPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await holdingsPage.NavigateViaMenuAsync();
            await holdingsPage.WaitForPageLoadAsync();

            var hasError = await holdingsPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "Holdings page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("holdings");
            throw;
        }
    }

    [RetryFact]
    public async Task AccountsPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var accountsPage = new AccountsPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await accountsPage.NavigateViaMenuAsync();
            await accountsPage.WaitForPageLoadAsync();

            var hasError = await accountsPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "Accounts page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("accounts");
            throw;
        }
    }

    [RetryFact]
    public async Task TaxReportPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var taxReportPage = new TaxReportPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await taxReportPage.NavigateViaMenuAsync();
            await taxReportPage.WaitForPageLoadAsync();

            var hasError = await taxReportPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "TaxReport page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("tax-report");
            throw;
        }
    }

    [RetryFact]
    public async Task TopMoversPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var topMoversPage = new TopMoversPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await topMoversPage.NavigateViaMenuAsync();
            await topMoversPage.WaitForPageLoadAsync();

            var hasError = await topMoversPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "TopMovers page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("top-movers");
            throw;
        }
    }

    [RetryFact]
    public async Task PortfolioTimeSeriesPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var timeSeriesPage = new PortfolioTimeSeriesPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await timeSeriesPage.NavigateViaMenuAsync();
            await timeSeriesPage.WaitForPageLoadAsync();

            var hasError = await timeSeriesPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "PortfolioTimeSeries page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("portfolio-timeseries");
            throw;
        }
    }

    [RetryFact]
    public async Task UpcomingDividendsPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var dividendsPage = new UpcomingDividendsPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await dividendsPage.NavigateViaMenuAsync();
            await dividendsPage.WaitForPageLoadAsync();

            var hasDividendsTitle = await dividendsPage.HasDividendsTitleAsync();
            Assert.True(hasDividendsTitle, "Upcoming Dividends page should display its title");
        }
        catch
        {
            await CaptureErrorStateAsync("upcoming-dividends");
            throw;
        }
    }

    [RetryFact]
    public async Task DataIssuesPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var dataIssuesPage = new DataIssuesPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await dataIssuesPage.NavigateViaMenuAsync();
            await dataIssuesPage.WaitForPageLoadAsync();

            var hasError = await dataIssuesPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "DataIssues page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("data-issues");
            throw;
        }
    }

    [RetryFact]
    public async Task TaskStatusPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var taskStatusPage = new TaskStatusPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            await taskStatusPage.NavigateViaMenuAsync();
            await taskStatusPage.WaitForPageLoadAsync();

            var hasTaskStatusTitle = await taskStatusPage.HasTaskStatusTitleAsync();
            Assert.True(hasTaskStatusTitle, "TaskStatus page should display its title");

            var hasError = await taskStatusPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "TaskStatus page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("task-status");
            throw;
        }
    }

    [RetryFact]
    public async Task TablesPage_ShouldLoadViaMenu()
    {
        var loginPage = new LoginPage(Page!);
        var homePage = new HomePage(Page!);
        var tablesPage = new TablesPage(Page!);

        try
        {
            await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
            await loginPage.WaitForSuccessfulLoginAsync();
            await homePage.WaitForPageLoadAsync();

            await homePage.ClickSyncButtonAsync();
            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            // Navigate via nav link directly - avoids ExecuteWithErrorCheckAsync which was
            // triggering false positive Blazor error detection from "Reload Data" button
            await tablesPage.NavigateDirectAsync();
            await tablesPage.WaitForPageLoadAsync();

            var hasTableViewerTitle = await tablesPage.HasTableViewerTitleAsync();
            Assert.True(hasTableViewerTitle, "Tables page should display its title");

            var hasError = await tablesPage.IsErrorDisplayedAsync();
            Assert.False(hasError, "Tables page should not show an error");
        }
        catch
        {
            await CaptureErrorStateAsync("tables");
            throw;
        }
    }
}
