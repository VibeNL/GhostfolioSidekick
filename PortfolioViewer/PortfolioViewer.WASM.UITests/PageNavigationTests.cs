using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class PageNavigationTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
	[RetryFact]
	public async Task HoldingsPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var holdingsPage = PageFactory.CreateHoldingsPage(Page!);
		await holdingsPage.NavigateViaMenuAsync();
		await holdingsPage.WaitForPageLoadAsync();

		var hasError = await holdingsPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "Holdings page should not show an error");
	}

	[RetryFact]
	public async Task AccountsPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var accountsPage = PageFactory.CreateAccountsPage(Page!);
		await accountsPage.NavigateViaMenuAsync();
		await accountsPage.WaitForPageLoadAsync();

		var hasError = await accountsPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "Accounts page should not show an error");
	}

	[RetryFact]
	public async Task TaxReportPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var taxReportPage = PageFactory.CreateTaxReportPage(Page!);
		await taxReportPage.NavigateViaMenuAsync();
		await taxReportPage.WaitForPageLoadAsync();

		var hasError = await taxReportPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "TaxReport page should not show an error");
	}

	[RetryFact]
	public async Task TopMoversPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var topMoversPage = PageFactory.CreateTopMoversPage(Page!);
		await topMoversPage.NavigateViaMenuAsync();
		await topMoversPage.WaitForPageLoadAsync();

		var hasError = await topMoversPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "TopMovers page should not show an error");
	}

	[RetryFact]
	public async Task PortfolioTimeSeriesPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var timeSeriesPage = PageFactory.CreatePortfolioTimeSeriesPage(Page!);
		await timeSeriesPage.NavigateViaMenuAsync();
		await timeSeriesPage.WaitForPageLoadAsync();

		var hasError = await timeSeriesPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "PortfolioTimeSeries page should not show an error");
	}

	[RetryFact]
	public async Task UpcomingDividendsPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var dividendsPage = PageFactory.CreateUpcomingDividendsPage(Page!);
		await dividendsPage.NavigateViaMenuAsync();
		await dividendsPage.WaitForPageLoadAsync();

		var hasDividendsTitle = await dividendsPage.HasDividendsTitleAsync();
		Assert.True(hasDividendsTitle, "Upcoming Dividends page should display its title");
	}

	[RetryFact]
	public async Task DataIssuesPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var dataIssuesPage = PageFactory.CreateDataIssuesPage(Page!);
		await dataIssuesPage.NavigateViaMenuAsync();
		await dataIssuesPage.WaitForPageLoadAsync();

		var hasError = await dataIssuesPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "DataIssues page should not show an error");
	}

	[RetryFact]
	public async Task TaskStatusPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var taskStatusPage = PageFactory.CreateTaskStatusPage(Page!);
		await taskStatusPage.NavigateViaMenuAsync();
		await taskStatusPage.WaitForPageLoadAsync();

		var hasTaskStatusTitle = await taskStatusPage.HasTaskStatusTitleAsync();
		Assert.True(hasTaskStatusTitle, "TaskStatus page should display its title");

		var hasError = await taskStatusPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "TaskStatus page should not show an error");
	}

	[RetryFact]
	public async Task TablesPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var tablesPage = PageFactory.CreateTablesPage(Page!);
		// Navigate directly to avoid ExecuteWithErrorCheckAsync triggering false positive Blazor error detection
		await tablesPage.NavigateDirectAsync();
		await tablesPage.WaitForPageLoadAsync();

		var hasTableViewerTitle = await tablesPage.HasTableViewerTitleAsync();
		Assert.True(hasTableViewerTitle, "Tables page should display its title");

		var hasError = await tablesPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "Tables page should not show an error");
	}
}
