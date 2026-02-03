using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests
{
	public class WasmUiSmokeTests(CustomWebApplicationFactory fixture) : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly string serverAddress = fixture.ServerAddress;

		[Fact]
		public async Task MainPage_ShouldLoadSuccessfully()
		{
			Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
			var videoDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-videos");
			Directory.CreateDirectory(videoDir);
			var context = await browser.NewContextAsync(new BrowserNewContextOptions
			{
				RecordVideoDir = videoDir
			});
			var page = await context.NewPageAsync();
			var screenshotDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright-screenshots");
			Directory.CreateDirectory(screenshotDir);
			string screenshotPath = Path.Combine(screenshotDir, $"mainpage-loaded-{DateTime.Now:yyyyMMddHHmmss}.png");
			string errorScreenshotPath = Path.Combine(screenshotDir, $"mainpage-error-{DateTime.Now:yyyyMMddHHmmss}.png");
			string errorHtmlPath = Path.Combine(screenshotDir, $"mainpage-error-{DateTime.Now:yyyyMMddHHmmss}.html");

			// Capture browser console logs for diagnostics
			page.Console += (_, msg) =>
			{
				Console.WriteLine($"[Browser Console] {msg.Type}: {msg.Text}");
			};

			try
			{
				// Navigate to the login page
				Console.WriteLine($"Navigating to: {serverAddress}");
				await page.GotoAsync(serverAddress);
				await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

				// Wait for the login form to appear
				await page.WaitForSelectorAsync("input#accessToken", new PageWaitForSelectorOptions { Timeout = 10000 });
				Console.WriteLine("Login form loaded successfully");

				// Fill in the access token
				await page.FillAsync("input#accessToken", CustomWebApplicationFactory.TestAccessToken);
				Console.WriteLine("Access token filled in");

				// Click the submit button
				await page.ClickAsync("button[type='submit']");
				Console.WriteLine("Login button clicked");

				// Wait for navigation after successful login (should redirect to home page)
				await page.WaitForURLAsync(url => url.Contains("/") && !url.Contains("/login"), new PageWaitForURLOptions { Timeout = 10000 });
				Console.WriteLine($"Successfully logged in and redirected to: {page.Url}");

				// Take a screenshot of the logged-in state
				await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });

				// Verify we're no longer on the login page
				Assert.DoesNotContain("/login", page.Url);
			}
			catch (Exception ex)
			{
				await page.ScreenshotAsync(new PageScreenshotOptions { Path = errorScreenshotPath });
				var html = await page.ContentAsync();
				await File.WriteAllTextAsync(errorHtmlPath, html, TestContext.Current.CancellationToken);
				Console.WriteLine($"Exception: {ex}");
				throw;
			}
			finally
			{
				await context.CloseAsync();
			}
		}

		[Fact]
		public async Task DebugApiHealthEndpoint()
		{
			// Log the API base address
			var apiClient = fixture.CreateDefaultClient();
			var baseAddress = apiClient.BaseAddress?.ToString() ?? "null";
			Console.WriteLine($"API BaseAddress: {baseAddress}");

			// Try to call the health endpoint directly
			var healthUrl = "api/auth/health";
			try
			{
				var response = await apiClient.GetAsync(healthUrl, TestContext.Current.CancellationToken);
				var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
				Console.WriteLine($"Status: {response.StatusCode}, Content: {content}");
				Assert.True(response.IsSuccessStatusCode, $"API health endpoint failed: {response.StatusCode} {content}");
			}
			catch (Exception ex)
			{
				Assert.Fail($"Exception calling API health endpoint: {ex}");
			}
		}
	}
}


