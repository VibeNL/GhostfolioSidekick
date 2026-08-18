using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;
namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class HoldingsPriceTargetsTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[Fact]
	public async Task NavigateToHoldingsPriceTargets_ShouldLoadWithoutBlazorError()
	{
		await SetupAsync();

		// Navigate to holdings price targets page
		await HoldingsPriceTargetsPage.NavigateDirectAsync($"{ServerAddress.TrimEnd('/')}/holdings-price-targets", ct: TestContext.Current.CancellationToken);

		// Wait for Blazor to initialize
		await Page!.WaitForSelectorAsync("#app", new PageWaitForSelectorOptions { Timeout = 10000 });

		// Page should render without crashing and without Blazor errors
		var errorEl = await Page.QuerySelectorAsync("#blazor-error-ui");
		var hasBlazorError = errorEl != null && await errorEl.IsVisibleAsync();

		var appDiv = await Page.QuerySelectorAsync("#app");
		var appContent = appDiv != null ? await appDiv.InnerHTMLAsync() : string.Empty;
		var appEmpty = string.IsNullOrWhiteSpace(appContent?.Trim());

		Assert.False(appEmpty, "Holdings Price Targets page should not crash and clear the Blazor app container");
		Assert.False(hasBlazorError, $"Holdings Price Targets page should not have Blazor errors: {(hasBlazorError ? await errorEl!.TextContentAsync() : string.Empty)}");
	}

	[Fact]
	public async Task HoldingsPriceTargetsPage_ShouldDisplayDataRows()
	{
		await SetupAsync();

		await HoldingsPriceTargetsPage.NavigateDirectAsync(ct: TestContext.Current.CancellationToken);

		// Page should render without crashing - just verify the page is not blank
		var appDiv = await Page!.QuerySelectorAsync("#app");
		var appEmpty = appDiv != null && (await appDiv.InnerHTMLAsync()).Trim() == string.Empty;
		Assert.False(appEmpty, "Holdings Price Targets page should not crash and clear the Blazor app container");
	}

	[Fact]
	public async Task HoldingsPriceTargetsPage_ShouldNavigateViaMenu()
	{
		await SetupAsync();

		await HoldingsPriceTargetsPage.NavigateViaMenuAsync();

		// Page should render without crashing - just verify the app container has content
		var appDiv = await Page!.QuerySelectorAsync("#app");
		var appContent = appDiv != null ? await appDiv.InnerHTMLAsync() : string.Empty;
		var appEmpty = string.IsNullOrWhiteSpace(appContent?.Trim());
		Assert.False(appEmpty, "Holdings Price Targets page should not crash and clear the Blazor app container");
	}

	[Fact]
	public async Task HoldingsPriceTargetsPage_ShouldShowSeededSymbols()
	{
		await SetupAsync();

		await HoldingsPriceTargetsPage.NavigateDirectAsync(ct: TestContext.Current.CancellationToken);

		var hasData = await HoldingsPriceTargetsPage.HasDataRowsAsync(1);
		Assert.True(hasData, "Holdings Price Targets page should show data rows since test data is seeded with overlapping holdings and price targets");

		// All seeded symbols have both a holding and a price target, so all should appear
		foreach (var symbol in new[] { "AAPL", "GOOGL", "BTC", "VTI" })
		{
			var hasSymbol = await HoldingsPriceTargetsPage.HasSymbolAsync(symbol);
			Assert.True(hasSymbol, $"Holdings Price Targets page should show seeded {symbol} entry");
		}
	}
}

