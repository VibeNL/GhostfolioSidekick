using Microsoft.Playwright;
using PortfolioViewer.WASM.UITests.PageObjects;

namespace PortfolioViewer.WASM.UITests
{
	public class WasmUiSmokeTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
	{
		[Fact]
		public async Task Login_ShouldSucceedWithValidToken()
		{
			var loginPage = new LoginPage(Page!);
			var homePage = new HomePage(Page!);

			try
			{
				Console.WriteLine($"Navigating to: {ServerAddress}");
				
				// Perform login
				await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
				Console.WriteLine("Login form filled and submitted");

				// Wait for successful redirect
				await loginPage.WaitForSuccessfulLoginAsync();
				Console.WriteLine($"Successfully logged in and redirected to: {Page!.Url}");

				// Verify we're on the home page
				await homePage.WaitForPageLoadAsync();
				
				// Take a screenshot of the logged-in state
				await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("login-success") });

				// Verify we're no longer on the login page
				Assert.False(loginPage.IsOnLoginPage(), "Should not be on login page after successful login");
			}
			catch (Exception ex)
			{
				await CaptureErrorStateAsync("login");
				Console.WriteLine($"Exception: {ex}");
				throw;
			}
		}

		[Fact]
		public async Task HomePage_ShouldDisplaySyncButton()
		{
			var loginPage = new LoginPage(Page!);
			var homePage = new HomePage(Page!);

			try
			{
				// Login first
				await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
				await loginPage.WaitForSuccessfulLoginAsync();
				Console.WriteLine("Login successful");

				// Wait for home page to load
				await homePage.WaitForPageLoadAsync();
				Console.WriteLine("Home page loaded");

				// Verify sync button is visible
				var isSyncButtonVisible = await homePage.IsSyncButtonVisibleAsync();
				Assert.True(isSyncButtonVisible, "Sync button should be visible on home page");

				// Take screenshot
				await Page!.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("homepage-loaded") });
			}
			catch (Exception ex)
			{
				await CaptureErrorStateAsync("homepage");
				Console.WriteLine($"Exception: {ex}");
				throw;
			}
		}

		[Fact]
		public async Task Sync_ShouldStartAndComplete()
		{
			var loginPage = new LoginPage(Page!);
			var homePage = new HomePage(Page!);

			try
			{
				// Login first
				await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
				await loginPage.WaitForSuccessfulLoginAsync();
				Console.WriteLine("Login successful");

				// Wait for home page to load
				await homePage.WaitForPageLoadAsync();
				Console.WriteLine("Home page loaded");

				// Check if this is first sync
				var hasNoSyncWarning = await homePage.HasNoSyncWarningAsync();
				Console.WriteLine(hasNoSyncWarning ? "First sync - no previous sync detected" : "Previous sync detected");

				// Verify sync button is enabled
				var isSyncButtonEnabled = await homePage.IsSyncButtonEnabledAsync();
				Assert.True(isSyncButtonEnabled, "Sync button should be enabled before starting sync");

				// Take screenshot before sync
				await Page!.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("before-sync") });

				// Start sync
				await homePage.ClickSyncButtonAsync();
				Console.WriteLine("Sync button clicked");

				// Wait a moment for sync to start
				await Page.WaitForTimeoutAsync(1000);

				// Verify sync is in progress
				var isSyncInProgress = await homePage.IsSyncInProgressAsync();
				Assert.True(isSyncInProgress, "Sync should be in progress after clicking sync button");
				Console.WriteLine("Sync in progress confirmed");

				// Take screenshot during sync
				await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("during-sync") });

				// Wait for sync to complete (with timeout)
				await homePage.WaitForSyncToCompleteAsync(timeout: 120000); // 2 minutes timeout
				Console.WriteLine("Sync completed");

				// Verify sync button is enabled again
				var isSyncButtonEnabledAfter = await homePage.IsSyncButtonEnabledAsync();
				Assert.True(isSyncButtonEnabledAfter, "Sync button should be enabled after sync completes");

				// Verify last sync time is now displayed
				var hasLastSyncTime = await homePage.HasLastSyncTimeAsync();
				Assert.True(hasLastSyncTime, "Last sync time should be displayed after successful sync");

				// Take screenshot after sync
				await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("after-sync") });
				Console.WriteLine("Sync test completed successfully");
			}
			catch (Exception ex)
			{
				await CaptureErrorStateAsync("sync");
				Console.WriteLine($"Exception: {ex}");
				throw;
			}
		}

		[Fact]
		public async Task DebugApiHealthEndpoint()
		{
			// Log the API base address
			var apiClient = Fixture.CreateDefaultClient();
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
