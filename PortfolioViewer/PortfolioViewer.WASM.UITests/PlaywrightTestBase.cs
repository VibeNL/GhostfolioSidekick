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

			Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
			Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

			VideoDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-videos");
			Directory.CreateDirectory(VideoDir);

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
	}
}
