using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class PriceTargetsTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
	[RetryFact]
	public async Task NavigateToPriceTargets_ShouldLoadWithoutBlazorError()
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
		var priceTargetsUrl = $"{ServerAddress.TrimEnd('/')}/price-targets";
		await PriceTargetsPage.NavigateDirectAsync(priceTargetsUrl);
		
		// Wait for Blazor to initialize
		await Page!.WaitForSelectorAsync("#app", new PageWaitForSelectorOptions { Timeout = 10000 });
		
		// Capture HTML for debugging before checking content
		var html = await Page.ContentAsync();
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "debug-pricetargets.html"), html, System.Text.Encoding.UTF8);

		// Check for Blazor errors first
		var errorEl = await Page.QuerySelectorAsync("#blazor-error-ui");
		if (errorEl != null && await errorEl.IsVisibleAsync())
		{
			var errorText = await errorEl.TextContentAsync() ?? "unknown error";
			Assert.Fail($"Blazor error on PriceTargets page: {errorText}");
		}

		// Wait for page content to render (any of: heading, empty state, error alert, or table)
		await Page.WaitForSelectorAsync("h5, .alert-danger, table, .spinner-border", 
			new PageWaitForSelectorOptions { Timeout = 30000 });

		var hasHeading = await PriceTargetsPage.HasPriceTargetsHeadingAsync();
		Assert.True(hasHeading, "Price Targets page should display the Analyst Price Targets heading. HTML preview: " + html.Substring(0, Math.Min(3000, html.Length)));
	}

	[RetryFact]
	public async Task PriceTargetsPage_ShouldDisplayDataRows()
	{
		await SetupAsync();

		await PriceTargetsPage.NavigateDirectAsync();
		await PriceTargetsPage.WaitForPageLoadAsync();

		var hasDataRows = await PriceTargetsPage.HasPriceTargetDataRowsAsync(1);
		Assert.True(hasDataRows, "Price Targets page should display data rows when price targets exist");
	}

	[RetryFact]
	public async Task PriceTargetsPage_ShouldShowSpecificSymbols()
	{
		await SetupAsync();

		await PriceTargetsPage.NavigateDirectAsync();
		await PriceTargetsPage.WaitForPageLoadAsync();

		var symbols = await PriceTargetsPage.GetPriceTargetSymbolsAsync();
		Assert.True(symbols.Any(s => s.Contains("AAPL", StringComparison.OrdinalIgnoreCase)), "Price Targets page should display AAPL symbol");
		Assert.True(symbols.Any(s => s.Contains("GOOGL", StringComparison.OrdinalIgnoreCase)), "Price Targets page should display GOOGL symbol");
	}

	[RetryFact]
	public async Task PriceTargetsPage_ShouldNavigateViaMenu()
	{
		await SetupAsync();

		await PriceTargetsPage.NavigateViaMenuAsync();
		await PriceTargetsPage.WaitForPageLoadAsync();

		var hasHeading = await PriceTargetsPage.HasPriceTargetsHeadingAsync();
		Assert.True(hasHeading, "Price Targets page should be displayed after navigation via menu");
	}
}
