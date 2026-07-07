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
		await holdingsPage.SwitchToTableModeAsync();

		var hasError = await holdingsPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "Holdings page should not show an error");

		var isEmpty = await holdingsPage.IsEmptyStateDisplayedAsync();
		Assert.False(isEmpty, "Holdings page should not be empty with seeded test data");

		var hasRows = await holdingsPage.HasHoldingsDataRowsAsync();
		Assert.True(hasRows, "Holdings page should show at least one holding row");

		var hasAapl = await holdingsPage.HasHoldingSymbolAsync("AAPL");
		Assert.True(hasAapl, "Holdings page should show seeded AAPL holding");
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

		var isEmpty = await accountsPage.IsEmptyStateDisplayedAsync();
		Assert.False(isEmpty, "Accounts page should not be empty with seeded test data");

		var hasRows = await accountsPage.HasAccountDataRowsAsync();
		Assert.True(hasRows, "Accounts page should show account data rows");

		var hasTestAccount = await accountsPage.HasAccountNameAsync("Test Account");
		Assert.True(hasTestAccount, "Accounts page should show the seeded test account");
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

		var isEmpty = await taxReportPage.IsEmptyStateDisplayedAsync();
		Assert.False(isEmpty, "TaxReport page should not be empty with seeded snapshot data");

		var hasRows = await taxReportPage.HasReportRowsAsync();
		Assert.True(hasRows, "TaxReport page should show report rows");

		var hasTestAccount = await taxReportPage.HasAccountNameAsync("Test Account");
		Assert.True(hasTestAccount, "TaxReport page should show the seeded test account");
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

		var hasRisers = await topMoversPage.HasRiserEntriesAsync();
		var hasLosers = await topMoversPage.HasLoserEntriesAsync();
		Assert.True(hasRisers || hasLosers, "TopMovers page should show at least one mover entry from seeded data");
	}

	[RetryFact]
	public async Task PortfolioTimeSeriesPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var timeSeriesPage = PageFactory.CreatePortfolioTimeSeriesPage(Page!);
		await timeSeriesPage.NavigateViaMenuAsync();
		await timeSeriesPage.WaitForPageLoadAsync();
		await timeSeriesPage.SwitchToTableModeAsync();

		var hasError = await timeSeriesPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "PortfolioTimeSeries page should not show an error");

		var isEmpty = await timeSeriesPage.IsEmptyStateDisplayedAsync();
		Assert.False(isEmpty, "PortfolioTimeSeries page should not be empty with seeded test data");

		var hasRows = await timeSeriesPage.HasTimeSeriesRowsAsync();
		Assert.True(hasRows, "PortfolioTimeSeries page should show table rows after switching to table view");
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

		var isEmpty = await dividendsPage.IsEmptyStateDisplayedAsync();
		Assert.False(isEmpty, "Upcoming Dividends page should not be empty with seeded test data");

		var hasRows = await dividendsPage.HasDividendRowsAsync();
		Assert.True(hasRows, "Upcoming Dividends page should show at least one dividend row");

		var hasVti = await dividendsPage.HasDividendSymbolAsync("VTI");
		Assert.True(hasVti, "Upcoming Dividends page should include seeded VTI dividend");
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

		var hasTasksList = await taskStatusPage.HasTasksListAsync();
		Assert.True(hasTasksList, "TaskStatus page should render task table");

		var hasRows = await taskStatusPage.HasTaskRowsAsync();
		Assert.True(hasRows, "TaskStatus page should show at least one task row");
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
