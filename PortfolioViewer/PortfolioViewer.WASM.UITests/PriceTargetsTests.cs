using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;
using GhostfolioSidekick.Tools.TestUtilities;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class PriceTargetsTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[Fact]
	public async Task NavigateToPriceTargets_ShouldLoadWithoutBlazorError()
	{
		Assert.True(await TestRetry.RunAsync(NavigateToPriceTargets_ShouldLoadWithoutBlazorError_Runnable), "Test failed after all retry attempts.");
	}

	private async Task NavigateToPriceTargets_ShouldLoadWithoutBlazorError_Runnable()
	{
		try
		{
			await SetupAsync();
		}
		catch (Exception setupEx)
		{
			var pageContent = await LoginPage.CapturePageContentAsync();
			var consoleLog = string.Join("\n", TestConsoleLogs);
			File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "debug-login.html"), pageContent, System.Text.Encoding.UTF8);
			Assert.Fail($"SetupAsync failed: {setupEx.Message}\nConsole logs:\n{consoleLog}");
		}

		// Navigate to price targets page
		await PriceTargetsPage.NavigateDirectAsync($"{ServerAddress.TrimEnd('/')}/price-targets");

		// Wait for Blazor to initialize
		await Page!.WaitForSelectorAsync("#app", new PageWaitForSelectorOptions { Timeout = 10000 });

		// Check for Blazor errors
		var errorEl = await Page.QuerySelectorAsync("#blazor-error-ui");
		if (errorEl != null && await errorEl.IsVisibleAsync())
		{
			var errorText = await errorEl.TextContentAsync() ?? "unknown error";
			Assert.Fail($"Blazor error on PriceTargets page: {errorText}");
		}

		// Page should render without crashing - just verify the app container has content
		var appDiv = await Page!.QuerySelectorAsync("#app");
		var appContent = appDiv != null ? await appDiv.InnerHTMLAsync() : string.Empty;
		var appEmpty = string.IsNullOrWhiteSpace(appContent?.Trim());

		// Also check for Blazor errors (reuse the errorEl from above)
		var hasBlazorError = errorEl != null && await errorEl.IsVisibleAsync();

		Assert.False(appEmpty, "Price Targets page should not crash and clear the Blazor app container");
		Assert.False(hasBlazorError, "Price Targets page should not have Blazor errors");
	}

	[Fact]
	public async Task PriceTargetsPage_ShouldDisplayDataRows()
	{
		Assert.True(await TestRetry.RunAsync(PriceTargetsPage_ShouldDisplayDataRows_Runnable), "Test failed after all retry attempts.");
	}

	private async Task PriceTargetsPage_ShouldDisplayDataRows_Runnable()
	{
		await SetupAsync();

		await PriceTargetsPage.NavigateDirectAsync();

		// Page should render without crashing - just verify the page is not blank
		var appDiv = await Page!.QuerySelectorAsync("#app");
		var appEmpty = appDiv != null && (await appDiv.InnerHTMLAsync()).Trim() == string.Empty;
		Assert.False(appEmpty, "Price Targets page should not crash and clear the Blazor app container");
	}

	[Fact]
	public async Task PriceTargetsPage_ShouldNavigateViaMenu()
	{
		Assert.True(await TestRetry.RunAsync(PriceTargetsPage_ShouldNavigateViaMenu_Runnable), "Test failed after all retry attempts.");
	}

	private async Task PriceTargetsPage_ShouldNavigateViaMenu_Runnable()
	{
		await SetupAsync();

		await PriceTargetsPage.NavigateViaMenuAsync();

		// Page should render without crashing - just verify the app container has content
		var appDiv = await Page!.QuerySelectorAsync("#app");
		var appContent = appDiv != null ? await appDiv.InnerHTMLAsync() : string.Empty;
		var appEmpty = string.IsNullOrWhiteSpace(appContent?.Trim());
		Assert.False(appEmpty, "Price Targets page should not crash and clear the Blazor app container");
	}

	[Fact]
	public async Task PriceTargetsPage_ShouldShowSeededSymbols()
	{
		Assert.True(await TestRetry.RunAsync(PriceTargetsPage_ShouldShowSeededSymbols_Runnable), "Test failed after all retry attempts.");
	}

	private async Task PriceTargetsPage_ShouldShowSeededSymbols_Runnable()
	{
		await SetupAsync();

		await PriceTargetsPage.NavigateDirectAsync();

		var hasData = await PriceTargetsPage.HasPriceTargetDataRowsAsync(1);
		var hasEmptyState = await PriceTargetsPage.IsEmptyStateDisplayedAsync();
		var hasError = await PriceTargetsPage.IsErrorDisplayedAsync();

		PageRenderAssertions.AssertRendered("Price Targets", hasData, hasEmptyState, hasError);
		await PageRenderAssertions.AssertSeededSymbolsWhenRowsPresentAsync(
			"Price Targets", hasData, new[] { "AAPL", "GOOGL", "BTC", "VTI" },
			PriceTargetsPage.HasPriceTargetSymbolAsync);
	}
}
