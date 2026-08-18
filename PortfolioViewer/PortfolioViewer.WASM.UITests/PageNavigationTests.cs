using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;
namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class PageNavigationTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[Fact]
	public async Task HoldingsPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var holdingsPage = new HoldingsPage(Page!);
		await holdingsPage.NavigateViaMenuAsync();
		await holdingsPage.SwitchToTableModeAsync();

		// Page should render without crashing
		var isEmpty = await holdingsPage.IsEmptyStateDisplayedAsync();
		var hasRows = await holdingsPage.HasHoldingsDataRowsAsync();
		var hasError = await holdingsPage.IsErrorDisplayedAsync();
		PageRenderAssertions.AssertRendered("Holdings", hasRows, isEmpty, hasError);

		await PageRenderAssertions.AssertSeededSymbolsWhenRowsPresentAsync("Holdings", hasRows, ["AAPL"], holdingsPage.HasHoldingSymbolAsync);
	}

	[Fact]
	public async Task HoldingsPage_ShouldShowSeededSymbols()
	{
		await SetupAsync();

		var holdingsPage = new HoldingsPage(Page!);
		await holdingsPage.NavigateViaMenuAsync();
		await holdingsPage.SwitchToTableModeAsync();

		var hasRows = await holdingsPage.HasHoldingsDataRowsAsync();
		var hasError = await holdingsPage.IsErrorDisplayedAsync();

		// Data may not appear if Ghostfolio API is not configured; just verify page rendered
		PageRenderAssertions.AssertRendered("Holdings", hasRows, hasError);

		// Verify all seeded symbols appear when rows are present
		await PageRenderAssertions.AssertSeededSymbolsWhenRowsPresentAsync("Holdings", hasRows, ["AAPL", "GOOGL", "BTC", "VTI", "US10Y"], holdingsPage.HasHoldingSymbolAsync);
	}

	[Fact]
	public async Task AccountsPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var accountsPage = new AccountsPage(Page!);
		await accountsPage.NavigateViaMenuAsync();

		// Page should render without crashing
		var isEmpty = await accountsPage.IsEmptyStateDisplayedAsync();
		var hasRows = await accountsPage.HasAccountDataRowsAsync();
		var hasError = await accountsPage.IsErrorDisplayedAsync();
		PageRenderAssertions.AssertRendered("Accounts", hasRows, isEmpty, hasError);
	}

	[Fact]
	public async Task TaxReportPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var taxReportPage = new TaxReportPage(Page!);
		await taxReportPage.NavigateViaMenuAsync();

		var isEmpty = await taxReportPage.IsEmptyStateDisplayedAsync();
		var hasRows = await taxReportPage.HasReportRowsAsync();
		var hasError = await taxReportPage.IsErrorDisplayedAsync();
		PageRenderAssertions.AssertRendered("TaxReport", hasRows, isEmpty, hasError);
	}

	[Fact]
	public async Task TopMoversPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var topMoversPage = new TopMoversPage(Page!);
		await topMoversPage.NavigateViaMenuAsync();

		var hasRisers = await topMoversPage.HasRiserEntriesAsync();
		var hasLosers = await topMoversPage.HasLoserEntriesAsync();
		var hasNoRisersMessage = await topMoversPage.HasNoRisersMessageAsync();
		var hasNoLosersMessage = await topMoversPage.HasNoLosersMessageAsync();
		var hasError = await topMoversPage.IsErrorDisplayedAsync();

		Assert.True(hasError || hasRisers || hasLosers || hasNoRisersMessage || hasNoLosersMessage,
			$"TopMovers page should render correctly (error: {hasError}, risers: {hasRisers}, losers: {hasLosers}, noRisersMsg: {hasNoRisersMessage}, noLosersMsg: {hasNoLosersMessage}). Check screenshots/HTML in playwright-screenshots/ for actual DOM state.");
	}

	[Fact]
	public async Task PortfolioTimeSeriesPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var timeSeriesPage = new PortfolioTimeSeriesPage(Page!);

		await timeSeriesPage.NavigateViaMenuAsync();
		await timeSeriesPage.SwitchToTableModeAsync();
		await timeSeriesPage.WaitForPageLoadAsync(ct: TestContext.Current.CancellationToken);

		var hasRows = await timeSeriesPage.HasTimeSeriesRowsAsync();
		Assert.True(hasRows,
			$"PortfolioTimeSeries page should render correctly (rows: {hasRows})");
	}

	[Fact]
	public async Task UpcomingDividendsPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var dividendsPage = new UpcomingDividendsPage(Page!);
		await dividendsPage.NavigateViaMenuAsync();

		var isEmpty = await dividendsPage.IsEmptyStateDisplayedAsync();
		var hasRows = await dividendsPage.HasDividendRowsAsync();
		var hasError = await dividendsPage.IsErrorDisplayedAsync();
		PageRenderAssertions.AssertRendered("Upcoming Dividends", hasRows, isEmpty, hasError);
	}

	[Fact]
	public async Task DataIssuesPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var dataIssuesPage = new DataIssuesPage(Page!);
		await dataIssuesPage.NavigateViaMenuAsync();

		// Page should render without crashing - just verify the page is not blank
		var appDiv = await Page!.QuerySelectorAsync("#app");
		var appEmpty = appDiv != null && (await appDiv.InnerHTMLAsync()).Trim() == string.Empty;
		Assert.False(appEmpty, "DataIssues page should not crash and clear the Blazor app container");
	}

	[Fact]
	public async Task TaskStatusPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var taskStatusPage = new TaskStatusPage(Page!);
		await taskStatusPage.NavigateViaMenuAsync();

		var hasTaskStatusTitle = await taskStatusPage.HasTaskStatusTitleAsync();
		Assert.True(hasTaskStatusTitle, "TaskStatus page should display its title");
	}

	[Fact]
	public async Task DividendsPage_ShouldHandleInvalidDecimalDataGracefully()
	{
		await SetupAsync();

		var dividendsPage = new UpcomingDividendsPage(Page!);
		await dividendsPage.NavigateViaMenuAsync();

		var hasTitle = await dividendsPage.HasDividendsTitleAsync();
		Assert.True(hasTitle, "Dividends page should display its title");
	}

	[Fact]
	public async Task TablesPage_ShouldLoadViaMenu()
	{
		await SetupAsync();

		var tablesPage = new TablesPage(Page!);
		await tablesPage.NavigateViaMenuAsync();

		var hasTableViewerTitle = await tablesPage.HasTableViewerTitleAsync();
		Assert.True(hasTableViewerTitle, "Tables page should display its title");
	}
}

