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

		[Fact]
		public async Task ComprehensiveSmokeTest_LoginSyncAndViewTransactions()
		{
			var loginPage = new LoginPage(Page!);
			var homePage = new HomePage(Page!);
			var transactionsPage = new TransactionsPage(Page!);

			try
			{
				// ====== STEP 1: Login ======
				Console.WriteLine("=== Step 1: Login ===");
				await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
				await loginPage.WaitForSuccessfulLoginAsync();
				Console.WriteLine("✓ Login successful");
				await Page!.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("comprehensive-01-login") });

				// ====== STEP 2: Verify Home Page ======
				Console.WriteLine("=== Step 2: Verify Home Page ===");
				await homePage.WaitForPageLoadAsync();
				var isSyncButtonVisible = await homePage.IsSyncButtonVisibleAsync();
				Assert.True(isSyncButtonVisible, "Sync button should be visible");
				Console.WriteLine("✓ Home page loaded");
				await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("comprehensive-02-homepage") });

				// ====== STEP 3: Start Sync ======
				Console.WriteLine("=== Step 3: Start Sync ===");
				var hasNoSyncWarning = await homePage.HasNoSyncWarningAsync();
				Console.WriteLine(hasNoSyncWarning ? "  First sync - will download all data" : "  Previous sync exists - will do partial sync");

				var isSyncButtonEnabled = await homePage.IsSyncButtonEnabledAsync();
				Assert.True(isSyncButtonEnabled, "Sync button should be enabled before starting sync");

				await homePage.ClickSyncButtonAsync();
				Console.WriteLine("✓ Sync started");
				await Page.WaitForTimeoutAsync(2000); // Give sync time to start

				// Verify sync is in progress
				var isSyncInProgress = await homePage.IsSyncInProgressAsync();
				Assert.True(isSyncInProgress, "Sync should be in progress");
				Console.WriteLine("✓ Sync in progress");
				await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("comprehensive-03-sync-started") });

				// ====== STEP 4: Monitor Sync Progress ======
				Console.WriteLine("=== Step 4: Monitor Sync Progress ===");
				var startTime = DateTime.Now;
				var lastProgress = 0;
				var progressChecks = 0;

				while (progressChecks < 120) // Max 2 minutes (120 checks * 1 second)
				{
					var progress = await homePage.GetProgressPercentageAsync();
					var currentAction = await homePage.GetCurrentActionAsync();
					
					if (progress != lastProgress)
					{
						Console.WriteLine($"  Progress: {progress}% - {currentAction}");
						lastProgress = progress;
					}

					if (progress == 100)
					{
						Console.WriteLine("✓ Sync reached 100%");
						break;
					}

					var isEnabled = await homePage.IsSyncButtonEnabledAsync();
					if (isEnabled && progress > 0)
					{
						Console.WriteLine("✓ Sync completed (button re-enabled)");
						break;
					}

					await Page.WaitForTimeoutAsync(1000);
					progressChecks++;
				}

				var elapsed = DateTime.Now - startTime;
				Console.WriteLine($"✓ Sync completed in {elapsed.TotalSeconds:F1} seconds");
				await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("comprehensive-04-sync-complete") });

				// Verify last sync time is displayed
				var hasLastSyncTime = await homePage.HasLastSyncTimeAsync();
				Assert.True(hasLastSyncTime, "Last sync time should be displayed after sync");
				Console.WriteLine("✓ Last sync time displayed");

				// ====== STEP 5: Navigate to Transactions ======
				Console.WriteLine("=== Step 5: Navigate to Transactions ===");
				await transactionsPage.NavigateViaMenuAsync();
				Console.WriteLine("✓ Navigated to transactions page");
				await Page.WaitForTimeoutAsync(1000); // Wait for navigation

				// ====== STEP 6: Wait for Transactions to Load ======
				Console.WriteLine("=== Step 6: Load Transactions ===");
				await transactionsPage.WaitForPageLoadAsync(timeout: 30000);
				Console.WriteLine("✓ Transactions page loaded");
				await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("comprehensive-05-transactions-loaded") });

				// ====== STEP 7: Verify Transaction Data ======
				Console.WriteLine("=== Step 7: Verify Transaction Data ===");
				
				// Check if we have transactions or empty state
				var hasTransactions = await transactionsPage.HasTransactionsAsync();
				var isEmpty = await transactionsPage.IsEmptyStateDisplayedAsync();
				var hasError = await transactionsPage.IsErrorDisplayedAsync();

				Console.WriteLine($"  Has transactions: {hasTransactions}");
				Console.WriteLine($"  Is empty state: {isEmpty}");
				Console.WriteLine($"  Has error: {hasError}");

				// We should either have transactions or empty state, but not an error
				Assert.False(hasError, "Transaction page should not show an error after successful sync");
				Assert.True(hasTransactions || isEmpty, "Transaction page should show either transactions or empty state");

				if (hasTransactions)
				{
					var transactionCount = await transactionsPage.GetTransactionCountAsync();
					Console.WriteLine($"✓ Found {transactionCount} transactions on current page");

					var totalText = await transactionsPage.GetTotalRecordsTextAsync();
					Console.WriteLine($"  Total records info: {totalText}");

					// Verify table is displayed
					var isTableDisplayed = await transactionsPage.IsTableDisplayedAsync();
					Assert.True(isTableDisplayed, "Transaction table should be visible");
					Console.WriteLine("✓ Transaction table is visible");

					// Get first few transactions for verification
					var transactions = await transactionsPage.GetTransactionRowsAsync(5);
					Console.WriteLine($"✓ Retrieved {transactions.Count} transaction details:");
					foreach (var transaction in transactions)
					{
						Console.WriteLine($"  {transaction}");
					}

					// Verify transaction data quality
					var hasValidData = await transactionsPage.VerifyTransactionDataAsync();
					Assert.True(hasValidData, "Transactions should have valid data (date, type, symbol)");
					Console.WriteLine("✓ Transaction data is valid");

					// Take final screenshot showing transaction data
					await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("comprehensive-06-transactions-verified") });
				}
				else if (isEmpty)
				{
					Console.WriteLine("⚠ No transactions found (empty state) - this might be expected if no data was synced");
					await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("comprehensive-06-transactions-empty") });
				}

				// ====== TEST COMPLETED ======
				Console.WriteLine("=== Comprehensive Smoke Test Completed Successfully ===");
				Console.WriteLine($"✓ Total test duration: {(DateTime.Now - startTime).TotalSeconds:F1} seconds");
			}
			catch (Exception ex)
			{
				await CaptureErrorStateAsync("comprehensive-smoketest");
				Console.WriteLine($"❌ Test failed: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
				throw;
			}
		}
	}
}
