using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;

namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Base class for Playwright UI tests. Uses shared browser from BrowserFixture.
/// Creates a new context per test for isolation. Provides login+sync setup and error checking.
/// </summary>
public abstract class PlaywrightTestBase : IAsyncLifetime
{
	protected readonly CustomWebApplicationFactory Fixture;
	protected readonly BrowserFixture BrowserFixture;
	protected string ServerAddress => Fixture.ServerAddress;

	// Screenshot control — set to true to enable step screenshots (default: false for speed)
	// Enable via: CaptureStepScreenshots = true; in test setup
	protected bool CaptureStepScreenshots { get; set; } = false;

	protected IPlaywright? Playwright;
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
	private HoldingsPriceTargetsPage? _holdingsPriceTargetsPage;
	protected LoginPage LoginPage => _loginPage ??= new LoginPage(Page!);
	protected HomePage HomePage => _homePage ??= new HomePage(Page!);
	protected PriceTargetsPage PriceTargetsPage => _priceTargetsPage ??= new PriceTargetsPage(Page!);
	protected HoldingsPriceTargetsPage HoldingsPriceTargetsPage => _holdingsPriceTargetsPage ??= new HoldingsPriceTargetsPage(Page!);

	protected static CancellationToken CancellationToken => TestContext.Current?.CancellationToken ?? CancellationToken.None;

	protected PlaywrightTestBase(CustomWebApplicationFactory fixture, BrowserFixture browserFixture)
	{
		Fixture = fixture;
		BrowserFixture = browserFixture;
	}

	public virtual async ValueTask InitializeAsync()
	{
		// Create a new context from the shared browser — no per-test browser startup
		var videoDir = string.Empty;
		var testName = GetCurrentTestName();
		if (!string.IsNullOrEmpty(testName))
		{
			var baseVideoDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-videos");
			VideoDir = Path.Combine(baseVideoDir, SanitizeFileName(testName));
			Directory.CreateDirectory(VideoDir);
			videoDir = VideoDir;
		}

		Context = await BrowserFixture.CreateContextAsync(videoDir);
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
	/// Captures a screenshot at every test step for debugging.
	/// Only captures if CaptureStepScreenshots is enabled (default: false for speed).
	/// Enable via: CaptureStepScreenshots = true; in test setup.
	/// </summary>
	protected async Task CaptureStepScreenshotAsync(string stepName)
	{
		if (Page == null || !CaptureStepScreenshots) return;

		var timestamp = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}";
		var path = Path.Combine(ScreenshotDir, $"{stepName}-{timestamp}.png");

		try
		{
			await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
			Console.WriteLine($"[Step Screenshot] {stepName} -> {path}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[Step Screenshot] Failed for '{stepName}': {ex.Message}");
		}
	}

	/// <summary>
	/// Captures comprehensive failure artifacts: screenshot, HTML, and console logs.
	/// Called automatically from DisposeAsync when TestContext reports a failed test.
	/// </summary>
	protected async Task CaptureFailureArtifacts(string testName)
	{
		if (Page == null) return;

		var timestamp = $"{DateTime.Now:yyyyMMdd-HHmmss}";
		var baseName = $"{testName}-failure-{timestamp}";

		// Capture full-page screenshot
		try
		{
			var screenshotPath = Path.Combine(ScreenshotDir, $"{baseName}.png");
			await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
			Console.WriteLine($"[Screenshot] Saved to: {screenshotPath}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[Screenshot] Failed to capture screenshot: {ex.Message}");
		}

		// Capture HTML source
		try
		{
			var htmlPath = Path.Combine(ScreenshotDir, $"{baseName}.html");
			var html = await Page.ContentAsync();
			await File.WriteAllTextAsync(htmlPath, html, CancellationToken);
			Console.WriteLine($"[HTML] Saved to: {htmlPath}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[HTML] Failed to capture HTML: {ex.Message}");
		}

		// Capture console logs to file
		if (TestConsoleLogs.Count > 0)
		{
			try
			{
				var logsPath = Path.Combine(ScreenshotDir, $"{baseName}-console.log");
				await File.WriteAllLinesAsync(logsPath, TestConsoleLogs, CancellationToken);
				Console.WriteLine($"[Console Logs] Saved {TestConsoleLogs.Count} entries to: {logsPath}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Console Logs] Failed to save logs: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Takes a manual screenshot during test execution.
	/// </summary>
	/// <param name="name">Descriptive name for the screenshot</param>
	/// <param name="fullPage">Whether to capture the full page or just the viewport</param>
	protected async Task<string> TakeScreenshotAsync(string name, bool fullPage = false)
	{
		if (Page == null) return string.Empty;

		var timestamp = $"{DateTime.Now:yyyyMMdd-HHmmss}";
		var path = Path.Combine(ScreenshotDir, $"{name}-{timestamp}.png");

		await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = fullPage });
		Console.WriteLine($"[Screenshot] {name} -> {path}");
		return path;
	}

	/// <summary>
	/// Takes a screenshot of a specific element.
	/// </summary>
	/// <param name="selector">CSS selector for the element to capture</param>
	/// <param name="name">Descriptive name for the screenshot</param>
	protected async Task<string> TakeElementScreenshotAsync(string selector, string name)
	{
		if (Page == null) return string.Empty;

		var element = await Page.QuerySelectorAsync(selector);
		if (element == null)
		{
			Console.WriteLine($"[Screenshot] Element not found: {selector}");
			return string.Empty;
		}

		var timestamp = $"{DateTime.Now:yyyyMMdd-HHmmss}";
		var path = Path.Combine(ScreenshotDir, $"{name}-{timestamp}.png");

		await element.ScreenshotAsync(new ElementHandleScreenshotOptions { Path = path });
		Console.WriteLine($"[Screenshot] {name} (element) -> {path}");
		return path;
	}

	/// <summary>
	/// Performs login and optionally syncs data. Called by derived tests before their assertions.
	/// Sync is skipped by default in test environments since data is already seeded in the database.
	/// </summary>
	protected async Task SetupAsync()
	{
		// Screenshot: Before login
		await CaptureStepScreenshotAsync("01-before-login");

		await LoginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken, CancellationToken);
		await LoginPage.WaitForSuccessfulLoginAsync();

		// Screenshot: After login
		await CaptureStepScreenshotAsync("02-after-login");

		await HomePage.WaitForPageLoadAsync(ct: CancellationToken);

		// Screenshot: After home page load
		await CaptureStepScreenshotAsync("03-home-loaded");

		// Click sync and wait for completion using WaitForFunction (no magic timeouts)
		var syncButton = await Page!.QuerySelectorAsync("button.btn-primary:has-text('Sync')");
		if (syncButton != null)
		{
			await syncButton.ClickAsync();
			// Wait for sync button to become enabled again (sync completed).
			// Wrapped in WaitAsync(CancellationToken) so a stuck sync aborts promptly via the test's
			// cancellation token instead of always blocking for the full 120s Playwright timeout -
			// this was a suspected contributor to the hangdump artifacts previously observed in TestResults.
			await Page.WaitForSelectorAsync("button.btn-primary:has-text('Sync'):not([disabled])", new PageWaitForSelectorOptions { Timeout = 120000 })
				.WaitAsync(CancellationToken);
		}

		// Screenshot: After sync
		await CaptureStepScreenshotAsync("04-after-sync");
	}

	public virtual async ValueTask DisposeAsync()
	{
		// Automatically capture debugging artifacts (screenshot, HTML, console log) when the test
		// failed. xUnit v3 populates TestContext.Current.TestState before DisposeAsync runs, so this
		// requires no per-test try/catch boilerplate to be "easily debuggable" out of the box.
		try
		{
			var testState = TestContext.Current?.TestState;
			if (testState?.Result == TestResult.Failed)
			{
				var testName = GetCurrentTestName();
				await CaptureFailureArtifacts(string.IsNullOrEmpty(testName) ? "UnknownTest" : testName);
				await CaptureErrorStateAsync(string.IsNullOrEmpty(testName) ? "UnknownTest" : testName);
			}
		}
		catch
		{
			// Never let artifact capture prevent test cleanup from completing.
		}

		// Dispose context (isolated per test) but NOT the shared browser
		if (Context != null)
		{
			await Context.CloseAsync();
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
