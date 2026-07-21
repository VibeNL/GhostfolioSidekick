using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class CsvExportTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[RetryFact]
	public async Task TransactionsPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/transactions");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on Transactions page");
	}

	[RetryFact]
	public async Task HoldingsPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/holdings");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on Holdings page");
	}

	[RetryFact]
	public async Task AccountsPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/accounts");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on Accounts page");
	}

	[RetryFact]
	public async Task DividendsPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/dividends");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on Dividends page");
	}

	[RetryFact]
	public async Task PortfolioTimeSeriesPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/portfolio-timeseries");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on PortfolioTimeSeries page");
	}

	[RetryFact]
	public async Task DataIssuesPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/data-issues");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on DataIssues page");
	}

	[RetryFact]
	public async Task TopMoversPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/top-movers");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on TopMovers page");
	}

	[RetryFact]
	public async Task TaxReportPage_HasExportButton()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/tax-report");
		await page.WaitForTimeoutAsync(1000);

		// Assert - verify export button exists and is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible on TaxReport page");
	}

	[RetryFact]
	public async Task ExportButton_VisibleWhenDataPresent()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/transactions");
		await page.WaitForTimeoutAsync(1000);

		// Verify table is present (data loaded)
		var table = await page.QuerySelectorAsync("table.table");
		Assert.NotNull(table);

		// Verify export button is visible
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);
		var isVisible = await exportButton!.IsVisibleAsync();
		Assert.True(isVisible, "Export CSV button should be visible");
	}

	[RetryFact]
	public async Task ExportButton_Clickable()
	{
		await SetupAsync();

		var page = Page!;
		await page.GotoAsync("/holdings");
		await page.WaitForTimeoutAsync(1000);

		// Verify export button is clickable
		var exportButton = await page.QuerySelectorAsync("button:has-text('Export CSV')");
		Assert.NotNull(exportButton);

		// Click the button - it should trigger the download (no exception)
		// In a browser test, the download will trigger but we can't verify the file
		// Just verify the click doesn't throw an error
		await exportButton!.ClickAsync();
	}
}
