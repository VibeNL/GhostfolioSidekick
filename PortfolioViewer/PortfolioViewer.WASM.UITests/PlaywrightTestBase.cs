using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests
{
	public abstract class PlaywrightTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
	{
		protected readonly CustomWebApplicationFactory Fixture;
		protected readonly string ServerAddress;
		protected IPlaywright? Playwright;
		protected IBrowser? Browser;
		protected IBrowserContext? Context;
		protected IPage? Page;

		protected string ScreenshotDir = string.Empty;
		protected string VideoDir = string.Empty;

		protected PlaywrightTestBase(CustomWebApplicationFactory fixture)
		{
			Fixture = fixture;
			ServerAddress = fixture.ServerAddress;
		}

		public virtual async ValueTask InitializeAsync()
		{
			Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

			// Get test name from TestContext if available
			var testName = GetCurrentTestName();

			Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
			Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

			// Create test-specific video directory
			var baseVideoDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-videos");
			VideoDir = !string.IsNullOrEmpty(testName) 
				? Path.Combine(baseVideoDir, SanitizeFileName(testName))
				: baseVideoDir;
			Directory.CreateDirectory(VideoDir);

			Console.WriteLine($"Recording video to: {VideoDir}");

			Context = await Browser.NewContextAsync(new BrowserNewContextOptions
			{
				RecordVideoDir = VideoDir
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
			await File.WriteAllTextAsync(GetErrorHtmlPath(testName), html, TestContext.Current.CancellationToken);
		}

		private string GetCurrentTestName()
		{
			try
			{
				// Try to get test name from xUnit TestContext
				var testContext = TestContext.Current;
				if (testContext != null)
				{
					// Use TestDisplayName which contains the full test name
					var testDisplayName = testContext.Test?.TestDisplayName;
					if (!string.IsNullOrEmpty(testDisplayName))
					{
						// Extract just the method name if it contains the full qualified name
						var lastDotIndex = testDisplayName.LastIndexOf('.');
						return lastDotIndex >= 0 ? testDisplayName[(lastDotIndex + 1)..] : testDisplayName;
					}
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
			// Remove invalid path characters
			var invalid = Path.GetInvalidFileNameChars();
			var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
			
			// Limit length to avoid path too long issues
			if (sanitized.Length > 100)
			{
				sanitized = sanitized[..100];
			}
			
			return sanitized;
		}
	}
}
