using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;

namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Base class for Playwright UI tests. Provides browser lifecycle, login+sync setup, and error checking.
/// </summary>
public abstract class PlaywrightTestBase : IAsyncLifetime
{
	protected readonly CustomWebApplicationFactory Fixture;
	protected string ServerAddress => Fixture.ServerAddress;

	protected IPlaywright? Playwright;
	protected IBrowser? Browser;
	protected IBrowserContext? Context;
	protected IPage? Page;

	protected string ScreenshotDir = string.Empty;
	protected string VideoDir = string.Empty;
	private List<string> _testConsoleLogs = [];
	protected IReadOnlyList<string> TestConsoleLogs => _testConsoleLogs;

	// Lazy-initialized page objects to avoid creating them before Page is available
	private LoginPage? _loginPage;
	private HomePage? _homePage;
	private PriceTargetsPage? _priceTargetsPage;
	protected LoginPage LoginPage => _loginPage ??= new LoginPage(Page!);
	protected HomePage HomePage => _homePage ??= new HomePage(Page!);
	protected PriceTargetsPage PriceTargetsPage => _priceTargetsPage ??= new PriceTargetsPage(Page!);

	protected static CancellationToken CancellationToken => TestContext.Current?.CancellationToken ?? CancellationToken.None;

	protected PlaywrightTestBase(CustomWebApplicationFactory fixture)
	{
		Fixture = fixture;
	}

	public virtual async ValueTask InitializeAsync()
	{
		Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

		// Create test-specific video directory
		var baseVideoDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-videos");
		var testName = GetCurrentTestName();
		VideoDir = !string.IsNullOrEmpty(testName)
			? Path.Combine(baseVideoDir, SanitizeFileName(testName))
			: baseVideoDir;
		Directory.CreateDirectory(VideoDir);

		Context = await Browser.NewContextAsync(new BrowserNewContextOptions
		{
			RecordVideoDir = VideoDir,
			ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
		});

		Page = await Context.NewPageAsync();

		ScreenshotDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-screenshots");
		Directory.CreateDirectory(ScreenshotDir);

		// Capture browser console logs for diagnostics
		Page.Console += (_, msg) =>
		{
			Console.WriteLine($"[Browser Console] {msg.Type}: {msg.Text}");
		};
	}

	/// <summary>
	/// Performs login and waits for sync to complete. Called by derived tests before their assertions.
	/// </summary>
	protected async Task SetupAsync(bool performSync = true, bool reseedAfterSync = false)
	{
		await LoginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken, CancellationToken);
		await LoginPage.WaitForSuccessfulLoginAsync();
		await HomePage.WaitForPageLoadAsync();

		if (performSync)
		{
			// Click sync and wait for completion using WaitForFunction (no magic timeouts)
			var syncButton = await Page!.QuerySelectorAsync("button.btn-primary:has-text('Sync')");
			if (syncButton != null)
			{
				await syncButton.ClickAsync();
				// Wait for sync button to become enabled again (sync completed)
				await Page.WaitForSelectorAsync("button.btn-primary:has-text('Sync'):not([disabled])", new PageWaitForSelectorOptions { Timeout = 120000 });
			}
		}

		if (reseedAfterSync)
		{
			Fixture.ResetAndReseedTestData();
		}
	}

	public virtual async ValueTask DisposeAsync()
	{
		if (Context != null)
		{
			await Context.CloseAsync();
		}

		if (Browser != null)
		{
			await Browser.CloseAsync();
		}

		Playwright?.Dispose();
	}

	protected string GetScreenshotPath(string name)
	{
		return Path.Combine(ScreenshotDir, $"{name}-{DateTime.Now:yyyyMMddHHmmss}.png");
	}

	protected string GetErrorScreenshotPath(string name)
	{
		return Path.Combine(ScreenshotDir, $"{name}-error-{DateTime.Now:yyyyMMddHHmmss}.png");
	}

	protected string GetErrorHtmlPath(string name)
	{
		return Path.Combine(ScreenshotDir, $"{name}-error-{DateTime.Now:yyyyMMddHHmmss}.html");
	}

	protected async Task CaptureErrorStateAsync(string testName)
	{
		if (Page == null) return;

		await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetErrorScreenshotPath(testName) });
		var html = await Page.ContentAsync();
		await File.WriteAllTextAsync(GetErrorHtmlPath(testName), html, CancellationToken);
	}

	private static string GetCurrentTestName()
	{
		try
		{
			var testDisplayName = TestContext.Current?.Test?.TestDisplayName;
			if (!string.IsNullOrEmpty(testDisplayName))
			{
				var lastDotIndex = testDisplayName.LastIndexOf('.');
				return lastDotIndex >= 0 ? testDisplayName[(lastDotIndex + 1)..] : testDisplayName;
			}
		}
		catch
		{
			// If we can't get the test name, return empty string
		}

		return string.Empty;
	}

	private static string SanitizeFileName(string fileName)
	{
		var invalid = Path.GetInvalidFileNameChars();
		var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

		if (sanitized.Length > 100)
		{
			sanitized = sanitized[..100];
		}

		return sanitized;
	}
}
