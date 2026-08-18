using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class HoldingsPriceTargetsTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[RetryFact]
	public async Task NavigateToHoldingsPriceTargets_ShouldLoadWithoutBlazorError()
	{
		await SetupAsync();

		// Navigate to holdings price targets page
		await HoldingsPriceTargetsPage.NavigateDirectAsync($"{ServerAddress.TrimEnd('/')}/holdings-price-targets");

		// Wait for Blazor to initialize
		await Page!.WaitForSelectorAsync("#app", new PageWaitForSelectorOptions { Timeout = 10000 });

		// Page should render without crashing and without Blazor errors
		var errorEl = await Page.QuerySelectorAsync("#blazor-error-ui");
		var hasBlazorError = errorEl != null && await errorEl.IsVisibleAsync();

		var hasRows = await HoldingsPriceTargetsPage.HasDataRowsAsync(1);
		var isEmpty = await HoldingsPriceTargetsPage.IsEmptyStateDisplayedAsync();
		var hasError = await HoldingsPriceTargetsPage.IsErrorDisplayedAsync();
		PageRenderAssertions.AssertRendered("Holdings Price Targets", hasRows, isEmpty, hasError);

		Assert.False(hasBlazorError, $"Holdings Price Targets page should not have Blazor errors: {(hasBlazorError ? await errorEl!.TextContentAsync() : string.Empty)}");
	}

	[RetryFact]
	public async Task HoldingsPriceTargetsPage_ShouldDisplayDataRows()
	{
		await SetupAsync();

		await HoldingsPriceTargetsPage.NavigateDirectAsync();

		var hasRows = await HoldingsPriceTargetsPage.HasDataRowsAsync(1);
		var isEmpty = await HoldingsPriceTargetsPage.IsEmptyStateDisplayedAsync();
		var hasError = await HoldingsPriceTargetsPage.IsErrorDisplayedAsync();
		PageRenderAssertions.AssertRendered("Holdings Price Targets", hasRows, isEmpty, hasError);
	}

	[RetryFact]
	public async Task HoldingsPriceTargetsPage_ShouldNavigateViaMenu()
	{
		await SetupAsync();

		await HoldingsPriceTargetsPage.NavigateViaMenuAsync();

		var hasRows = await HoldingsPriceTargetsPage.HasDataRowsAsync(1);
		var isEmpty = await HoldingsPriceTargetsPage.IsEmptyStateDisplayedAsync();
		var hasError = await HoldingsPriceTargetsPage.IsErrorDisplayedAsync();
		PageRenderAssertions.AssertRendered("Holdings Price Targets", hasRows, isEmpty, hasError);
	}

	[RetryFact]
	public async Task HoldingsPriceTargetsPage_ShouldShowSeededSymbols()
	{
		await SetupAsync();

		await HoldingsPriceTargetsPage.NavigateDirectAsync();

		var hasData = await HoldingsPriceTargetsPage.HasDataRowsAsync(1);
		Assert.True(hasData, "Holdings Price Targets page should show data rows since test data is seeded with overlapping holdings and price targets");

		// All seeded symbols have both a holding and a price target, so all should appear
		await PageRenderAssertions.AssertSeededSymbolsWhenRowsPresentAsync(
			"Holdings Price Targets", hasData, new[] { "AAPL", "GOOGL", "BTC", "VTI" },
			HoldingsPriceTargetsPage.HasSymbolAsync);
	}
}
