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
		/// Captures a screenshot at every test step for debugging.
		/// Automatically called after each major test operation.
		/// </summary>
		protected async Task CaptureStepScreenshotAsync(string stepName)
		{
			if (Page == null) return;

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
		/// Called automatically when a test fails.
		/// </summary>
		protected async void CaptureFailureArtifacts(string testName)
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
	protected async Task SetupAsync(bool performSync = false, bool reseedAfterSync = false)
	{
		// Screenshot: Before login
		await CaptureStepScreenshotAsync("01-before-login");

		await LoginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken, CancellationToken);
		await LoginPage.WaitForSuccessfulLoginAsync();

		// Screenshot: After login
		await CaptureStepScreenshotAsync("02-after-login");

		await HomePage.WaitForPageLoadAsync();

		// Screenshot: After home page load
		await CaptureStepScreenshotAsync("03-home-loaded");

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

			// Screenshot: After sync
			await CaptureStepScreenshotAsync("04-after-sync");
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
