using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class PageNavigationTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
	[RetryFact]
	public async Task HoldingsPage_ShouldLoadViaMenu()
	{
		await SetupAsync(reseedAfterSync: true);

		var holdingsPage = PageFactory.CreateHoldingsPage(Page!);
		await holdingsPage.NavigateViaMenuAsync();
		await holdingsPage.WaitForPageLoadAsync();
		await holdingsPage.SwitchToTableModeAsync();

		var hasError = await holdingsPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "Holdings page should not show an error");

		var isEmpty = await holdingsPage.IsEmptyStateDisplayedAsync();
		var hasRows = await holdingsPage.HasHoldingsDataRowsAsync();
		Assert.True(hasRows || isEmpty, "Holdings page should show holdings rows or an explicit empty state");

		if (hasRows)
		{
			var hasAapl = await holdingsPage.HasHoldingSymbolAsync("AAPL");
			Assert.True(hasAapl, "Holdings page should show seeded AAPL holding when data rows are present");
		}
	}

	[RetryFact]
	public async Task AccountsPage_ShouldLoadViaMenu()
	{
		await SetupAsync(reseedAfterSync: true);

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
		await SetupAsync(reseedAfterSync: true);

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
		await SetupAsync(reseedAfterSync: true);

		var topMoversPage = PageFactory.CreateTopMoversPage(Page!);
		await topMoversPage.NavigateViaMenuAsync();
		await topMoversPage.WaitForPageLoadAsync();

		var hasError = await topMoversPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "TopMovers page should not show an error");

		// With seeded test data (holdings with calculated snapshots), the page should show risers/losers
		var hasRisers = await topMoversPage.HasRiserEntriesAsync();
		var hasNoRisersMessage = await topMoversPage.HasNoRisersMessageAsync();
		Assert.True(hasRisers || hasNoRisersMessage, "TopMovers page should show risers or explicit no-risers messaging");

		var hasLosers = await topMoversPage.HasLoserEntriesAsync();
		var hasNoLosersMessage = await topMoversPage.HasNoLosersMessageAsync();
		Assert.True(hasLosers || hasNoLosersMessage, "TopMovers page should show losers or explicit no-loser message");

		// With seeded test data, at least one of risers/losers should be non-empty
		Assert.True(hasRisers || hasLosers, "TopMovers page should show risers or losers with seeded test data (not both empty)");
	}

	[RetryFact]
	public async Task PortfolioTimeSeriesPage_ShouldLoadViaMenu()
	{
		await SetupAsync(reseedAfterSync: true);

		var timeSeriesPage = PageFactory.CreatePortfolioTimeSeriesPage(Page!);
		await timeSeriesPage.NavigateViaMenuAsync();
		await timeSeriesPage.WaitForPageLoadAsync();
		await timeSeriesPage.SwitchToTableModeAsync();

		var hasError = await timeSeriesPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "PortfolioTimeSeries page should not show an error");

		var isEmpty = await timeSeriesPage.IsEmptyStateDisplayedAsync();
		var hasRows = await timeSeriesPage.HasTimeSeriesRowsAsync();
		Assert.True(hasRows || isEmpty, "PortfolioTimeSeries page should show timeline rows or explicit empty state");
	}

	[RetryFact]
	public async Task UpcomingDividendsPage_ShouldLoadViaMenu()
	{
		await SetupAsync(reseedAfterSync: true);

		var dividendsPage = PageFactory.CreateUpcomingDividendsPage(Page!);
		await dividendsPage.NavigateViaMenuAsync();
		await dividendsPage.WaitForPageLoadAsync();

		// Verify no Blazor errors occurred during page load (catches FormatException, etc.)
		var hasError = await dividendsPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "Upcoming Dividends page should not show a Blazor error during load");

		var hasDividendsTitle = await dividendsPage.HasDividendsTitleAsync();
		Assert.True(hasDividendsTitle, "Upcoming Dividends page should display its title");

		var isEmpty = await dividendsPage.IsEmptyStateDisplayedAsync();
		var hasRows = await dividendsPage.HasDividendRowsAsync();
		Assert.True(hasRows || isEmpty, "Upcoming Dividends page should show dividend rows or explicit empty state");

		// With seeded test data, the page should be non-empty
		Assert.False(isEmpty, "Upcoming Dividends page should not be empty with seeded upcoming dividend test data");

		// With seeded test data, must have data rows (not just the empty state)
		Assert.True(hasRows, "Upcoming Dividends page should show dividend data rows with seeded test data");

		var hasVti = await dividendsPage.HasDividendSymbolAsync("VTI");
		Assert.True(hasVti, "Upcoming Dividends page should include seeded VTI dividend when rows are present");
	}

	[RetryFact]
	public async Task DataIssuesPage_ShouldLoadViaMenu()
	{
		await SetupAsync(reseedAfterSync: true);

		var dataIssuesPage = PageFactory.CreateDataIssuesPage(Page!);
		await dataIssuesPage.NavigateViaMenuAsync();
		await dataIssuesPage.WaitForPageLoadAsync();

		var hasError = await dataIssuesPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "DataIssues page should not show an error");

		// With seeded test data (all activities have holdings), the page should show the "No Data Issues Found" success message
		var hasNoIssuesMessage = await dataIssuesPage.HasNoIssuesMessageAsync();
		Assert.True(hasNoIssuesMessage, "DataIssues page should display 'No Data Issues Found' when all activities have valid holdings");
	}

	[RetryFact]
	public async Task TaskStatusPage_ShouldLoadViaMenu()
	{
		await SetupAsync(reseedAfterSync: true);

		var taskStatusPage = PageFactory.CreateTaskStatusPage(Page!);
		await taskStatusPage.NavigateViaMenuAsync();
		await taskStatusPage.WaitForPageLoadAsync();

		var hasTaskStatusTitle = await taskStatusPage.HasTaskStatusTitleAsync();
		Assert.True(hasTaskStatusTitle, "TaskStatus page should display its title");

		var hasError = await taskStatusPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "TaskStatus page should not show an error");

		// With seeded test data, the page should show task rows (not the "no task data" message)
		var hasNoTaskDataMessage = await taskStatusPage.HasNoTaskDataMessageAsync();
		Assert.False(hasNoTaskDataMessage, "TaskStatus page should not show 'no task data' message with seeded test data");

		var hasTaskRows = await taskStatusPage.HasTaskRowsAsync();
		Assert.True(hasTaskRows, "TaskStatus page should show task rows with seeded test data");
	}

	[RetryFact]
	public async Task DividendsPage_ShouldHandleInvalidDecimalDataGracefully()
	{
		await SetupAsync(reseedAfterSync: true);

		// Seed invalid decimal data directly into SQLite (empty string in Amount column)
		Fixture.SeedInvalidDividendData();

		var dividendsPage = PageFactory.CreateUpcomingDividendsPage(Page!);
		await dividendsPage.NavigateDirectAsync(ServerAddress, CancellationToken);

		// Wait for the page to settle
		await dividendsPage.WaitForPageLoadAsync(timeout: 10000);

		// The page should NOT crash — the corrupted data should be handled gracefully
		// Check that the Blazor app container is not empty (indicates unhandled exception)
		var appDiv = await Page!.QuerySelectorAsync("#app");
		var appEmpty = appDiv != null && (await appDiv.InnerHTMLAsync()).Trim() == string.Empty;

		var errorUi = await Page!.QuerySelectorAsync("#blazor-error-ui");
		var errorVisible = errorUi != null && await errorUi.IsVisibleAsync();

		Assert.False(appEmpty, "Dividends page should not crash and clear the Blazor app container when handling invalid decimal data");
		Assert.False(errorVisible, "Dividends page should not show Blazor error UI when handling invalid decimal data");
	}

	[RetryFact]
	public async Task TablesPage_ShouldLoadViaMenu()
	{
		await SetupAsync(reseedAfterSync: true);

		var tablesPage = PageFactory.CreateTablesPage(Page!);
		// Navigate directly to avoid ExecuteWithErrorCheckAsync triggering false positive Blazor error detection
		await tablesPage.NavigateDirectAsync();
		await tablesPage.WaitForPageLoadAsync();

		var hasTableViewerTitle = await tablesPage.HasTableViewerTitleAsync();
		Assert.True(hasTableViewerTitle, "Tables page should display its title");

		var hasError = await tablesPage.IsErrorDisplayedAsync();
		Assert.False(hasError, "Tables page should not show an error");

		// With seeded test data, selecting a table should show loaded data
		var hasTableSelector = await tablesPage.HasTableSelectorAsync();
		Assert.True(hasTableSelector, "Tables page should have a table selector dropdown");

		// Select the first available table and verify data loaded
		if (hasTableSelector)
		{
			// Get available table options and select the first one
			var options = await Page!.QuerySelectorAllAsync("#tableSelect option");
			if (options.Count > 1) // Skip the default empty option
			{
				var firstTableName = await options[1].GetAttributeAsync("value") ?? "";
				if (string.IsNullOrWhiteSpace(firstTableName))
				{
					// Fallback: use the text content
					var text = await options[1].TextContentAsync();
					firstTableName = text?.Trim() ?? "";
				}
				firstTableName = firstTableName.Trim();
				await tablesPage.SelectTableAsync(firstTableName);
				await tablesPage.WaitForPageLoadAsync();

				var hasDataLoaded = await tablesPage.HasDataLoadedAsync();
				Assert.True(hasDataLoaded, "Tables page should show data when a table is selected with seeded test data");
			}
		}
	}
}
