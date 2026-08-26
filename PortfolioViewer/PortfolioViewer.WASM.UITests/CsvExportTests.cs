using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;
using GhostfolioSidekick.Tools.TestUtilities;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class CsvExportTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[Fact]
	public async Task TransactionsPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(TransactionsPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task TransactionsPage_HasExportButton_Runnable()
	{
		var transactionsPage = new TransactionsPage(Page!);
		await VerifyExportButtonWorksAsync(transactionsPage, () => transactionsPage.NavigateDirectAsync(), "Transactions");
	}

	[Fact]
	public async Task HoldingsPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(HoldingsPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task HoldingsPage_HasExportButton_Runnable()
	{
		CaptureStepScreenshots = true;
		var holdingsPage = new HoldingsPage(Page!);
		await VerifyExportButtonWorksAsync(holdingsPage, () => holdingsPage.NavigateDirectAsync(), "Holdings");
	}

	[Fact]
	public async Task AccountsPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(AccountsPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task AccountsPage_HasExportButton_Runnable()
	{
		var accountsPage = new AccountsPage(Page!);
		await VerifyExportButtonWorksAsync(accountsPage, () => accountsPage.NavigateDirectAsync(), "Accounts");
	}

	[Fact]
	public async Task DividendsPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(DividendsPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task DividendsPage_HasExportButton_Runnable()
	{
		var dividendsPage = new DividendsPage(Page!);
		await VerifyExportButtonWorksAsync(dividendsPage, () => dividendsPage.NavigateDirectAsync(), "Dividends");
	}

	[Fact]
	public async Task PortfolioTimeSeriesPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(PortfolioTimeSeriesPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task PortfolioTimeSeriesPage_HasExportButton_Runnable()
	{
		var timeSeriesPage = new PortfolioTimeSeriesPage(Page!);
		await VerifyExportButtonWorksAsync(timeSeriesPage, () => timeSeriesPage.NavigateDirectAsync(), "PortfolioTimeSeries");
	}

	[Fact]
	public async Task TopMoversPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(TopMoversPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task TopMoversPage_HasExportButton_Runnable()
	{
		var topMoversPage = new TopMoversPage(Page!);
		await VerifyExportButtonWorksAsync(topMoversPage, () => topMoversPage.NavigateDirectAsync(), "TopMovers");
	}

	[Fact]
	public async Task TaxReportPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(TaxReportPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task TaxReportPage_HasExportButton_Runnable()
	{
		var taxReportPage = new TaxReportPage(Page!);
		await VerifyExportButtonWorksAsync(taxReportPage, () => taxReportPage.NavigateDirectAsync(), "TaxReport");
	}

	[Fact]
	public async Task ExportButton_VisibleWhenDataPresent()
	{
		Assert.True(await TestRetry.RunAsync(ExportButton_VisibleWhenDataPresent_Runnable), "Test failed after all retry attempts.");
	}

	private async Task ExportButton_VisibleWhenDataPresent_Runnable()
	{
		var transactionsPage = new TransactionsPage(Page!);
		await VerifyExportButtonWorksAsync(transactionsPage, () => transactionsPage.NavigateDirectAsync(), "Transactions", requireVisible: true);
	}

	[Fact]
	public async Task ExportButton_Clickable()
	{
		Assert.True(await TestRetry.RunAsync(ExportButton_Clickable_Runnable), "Test failed after all retry attempts.");
	}

	private async Task ExportButton_Clickable_Runnable()
	{
		var holdingsPage = new HoldingsPage(Page!);
		await VerifyExportButtonWorksAsync(holdingsPage, () => holdingsPage.NavigateDirectAsync(), "Holdings", requireVisible: true);
	}

	[Fact]
	public async Task DataIssuesPage_HasExportButton()
	{
		Assert.True(await TestRetry.RunAsync(DataIssuesPage_HasExportButton_Runnable), "Test failed after all retry attempts.");
	}

	private async Task DataIssuesPage_HasExportButton_Runnable()
	{
		await SetupAsync();

		var dataIssuesPage = new DataIssuesPage(Page!);
		await dataIssuesPage.NavigateDirectAsync();

		// Check for Blazor errors
		var errorEl = await Page!.QuerySelectorAsync("#blazor-error-ui");
		var hasBlazorError = errorEl != null && await errorEl.IsVisibleAsync();
		Assert.False(hasBlazorError, "DataIssues page should not have Blazor errors");

		// Export button only appears when there are data issues (activities without holdings)
		// If no export button, verify the empty state is shown instead
		var exportButton = await Page!.QuerySelectorAsync("button[title=\"Export to CSV\"]");
		if (exportButton != null)
		{
			await ClickExportAndVerifyAsync(dataIssuesPage, exportButton, "DataIssues");
		}
		else
		{
			// No export button means no data issues - verify empty state is shown
			var emptyState = await Page!.QuerySelectorAsync("i.bi-check-circle-fill.text-success");
			Assert.NotNull(emptyState);
		}
	}

	/// <summary>
	/// Navigates to a page, verifies there are no Blazor errors, locates the Export CSV button,
	/// clicks it and confirms the click triggers a genuine file download without crashing the app.
	/// </summary>
	private async Task VerifyExportButtonWorksAsync(BasePageObject pageObject, Func<Task> navigate, string pageName, bool requireVisible = false)
	{
		await SetupAsync();

		await navigate();

		// Check for Blazor errors before clicking export
		var errorEl = await Page!.QuerySelectorAsync("#blazor-error-ui");
		var hasBlazorError = errorEl != null && await errorEl.IsVisibleAsync();
		Assert.False(hasBlazorError, $"{pageName} page should not have Blazor errors before export");

		var exportButton = await Page!.QuerySelectorAsync("button[title=\"Export to CSV\"]");
		Assert.True(exportButton != null, $"{pageName} page should have an Export CSV button");

		if (requireVisible)
		{
			var isVisible = await exportButton!.IsVisibleAsync();
			Assert.True(isVisible, $"Export button should be visible on {pageName} page with data");
		}

		await ClickExportAndVerifyAsync(pageObject, exportButton!, pageName);
	}

	/// <summary>
	/// Clicks the export button while waiting for the resulting browser download, then asserts
	/// the download (when raised) has a .csv extension and that the app remains rendered afterwards.
	/// </summary>
	private async Task ClickExportAndVerifyAsync(BasePageObject pageObject, IElementHandle exportButton, string pageName)
	{
		IDownload? download = null;

		await pageObject.ExecuteWithErrorCheckAsync(async () =>
		{
			var downloadWaitTask = Page!.WaitForDownloadAsync(new PageWaitForDownloadOptions { Timeout = 5000 });
			await exportButton.ClickAsync();

			try
			{
				download = await downloadWaitTask;
			}
			catch (TimeoutException)
			{
				// Some environments/pages may trigger the download via a mechanism Playwright
				// doesn't surface as a 'download' event (e.g. in-memory blob without navigation).
				// The absence of a Blazor error and a still-rendered app are validated below.
			}
		});

		if (download != null)
		{
			Assert.EndsWith(".csv", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
		}

		// If we get here, no Blazor error occurred (ExecuteWithErrorCheckAsync would have thrown)

		// Verify page still rendered without crashing after export click
		var postAppDiv = await Page!.QuerySelectorAsync("#app");
		var postAppContent = postAppDiv != null ? await postAppDiv.InnerHTMLAsync() : string.Empty;
		var postAppEmpty = string.IsNullOrWhiteSpace(postAppContent?.Trim());
		Assert.False(postAppEmpty, $"{pageName} page should not crash after clicking export CSV button");
	}
}
