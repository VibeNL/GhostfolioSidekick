using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Shared browser lifecycle fixture — one Chromium instance for the entire test run.
/// Avoids per-test browser startup overhead (~5-10s per test).
/// Tests create isolated contexts via CreateContextAsync().
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
	public IBrowser? Browser { get; private set; }

	public async ValueTask InitializeAsync()
	{
		var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
	}

	public async ValueTask DisposeAsync()
	{
		if (Browser != null)
		{
			await Browser.CloseAsync();
		}
	}

	/// <summary>
	/// Creates a new isolated browser context for a test (cookies/storage isolated per context).
	/// </summary>
	public async Task<IBrowserContext> CreateContextAsync(string? videoDir = null)
	{
		if (Browser == null) throw new InvalidOperationException("Browser not initialized");
		return await Browser.NewContextAsync(new BrowserNewContextOptions
		{
			ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
			RecordVideoDir = videoDir
		});
	}
}
