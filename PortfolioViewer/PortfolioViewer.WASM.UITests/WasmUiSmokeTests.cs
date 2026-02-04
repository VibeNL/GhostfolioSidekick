using PortfolioViewer.WASM.UITests.PageObjects;

namespace PortfolioViewer.WASM.UITests
{
	public class WasmUiSmokeTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
	{

		[Fact]
		public async Task Api_HealthEndpoint_GivesResponse()
		{
			// Log the API base address
			var apiClient = Fixture.CreateDefaultClient();
			
			// Try to call the health endpoint directly
			var healthUrl = "api/auth/health";
			try
			{
				var response = await apiClient.GetAsync(healthUrl, TestContext.Current.CancellationToken);
				var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
				Assert.True(response.IsSuccessStatusCode, $"API health endpoint failed: {response.StatusCode} {content}");
			}
			catch (Exception ex)
			{
				Assert.Fail($"Exception calling API health endpoint: {ex}");
			}
		}

		[Fact]
		public async Task Login_ShouldSucceedWithValidToken()
		{
			var loginPage = new LoginPage(Page!);
			var homePage = new HomePage(Page!);

			try
			{
				await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
				await loginPage.WaitForSuccessfulLoginAsync();
				await homePage.WaitForPageLoadAsync();

				Assert.False(loginPage.IsOnLoginPage(), "Should not be on login page after successful login");
			}
			catch
			{
				await CaptureErrorStateAsync("login");
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
				await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
				await loginPage.WaitForSuccessfulLoginAsync();
				await homePage.WaitForPageLoadAsync();

				var isSyncButtonEnabled = await homePage.IsSyncButtonEnabledAsync();
				Assert.True(isSyncButtonEnabled, "Sync button should be enabled before starting sync");

				await homePage.ClickSyncButtonAsync();
				await Page!.WaitForTimeoutAsync(1000);

				var isSyncInProgress = await homePage.IsSyncInProgressAsync();
				Assert.True(isSyncInProgress, "Sync should be in progress after clicking sync button");

				await homePage.WaitForSyncToCompleteAsync(timeout: 120000);

				var isSyncButtonEnabledAfter = await homePage.IsSyncButtonEnabledAsync();
				Assert.True(isSyncButtonEnabledAfter, "Sync button should be enabled after sync completes");

				var hasLastSyncTime = await homePage.HasLastSyncTimeAsync();
				Assert.True(hasLastSyncTime, "Last sync time should be displayed after successful sync");
			}
			catch
			{
				await CaptureErrorStateAsync("sync");
				throw;
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
				await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
				await loginPage.WaitForSuccessfulLoginAsync();

				await homePage.WaitForPageLoadAsync();
				var isSyncButtonVisible = await homePage.IsSyncButtonVisibleAsync();
				Assert.True(isSyncButtonVisible, "Sync button should be visible");

				var isSyncButtonEnabled = await homePage.IsSyncButtonEnabledAsync();
				Assert.True(isSyncButtonEnabled, "Sync button should be enabled before starting sync");

				await homePage.ClickSyncButtonAsync();
				await Page!.WaitForTimeoutAsync(2000);

				var isSyncInProgress = await homePage.IsSyncInProgressAsync();
				Assert.True(isSyncInProgress, "Sync should be in progress");

				await homePage.WaitForSyncToCompleteAsync(timeout: 120000);

				var hasLastSyncTime = await homePage.HasLastSyncTimeAsync();
				Assert.True(hasLastSyncTime, "Last sync time should be displayed after sync");

				await transactionsPage.NavigateViaMenuAsync();
				await Page.WaitForTimeoutAsync(1000);

				await transactionsPage.WaitForPageLoadAsync(timeout: 30000);

			var hasTransactions = await transactionsPage.HasTransactionsAsync();
			var isEmpty = await transactionsPage.IsEmptyStateDisplayedAsync();
			var hasError = await transactionsPage.IsErrorDisplayedAsync();

			Assert.False(hasError, "Transaction page should not show an error after successful sync");
			Assert.True(hasTransactions, "Transaction page should show transactions after successful sync (test data should be seeded)");
			Assert.False(isEmpty, "Transaction page should not be empty after successful sync with seeded test data");

			var isTableDisplayed = await transactionsPage.IsTableDisplayedAsync();
			Assert.True(isTableDisplayed, "Transaction table should be visible");

			var hasValidData = await transactionsPage.VerifyTransactionDataAsync();
			Assert.True(hasValidData, "Transactions should have valid data (date, type, symbol)");
			}
			catch
			{
				await CaptureErrorStateAsync("comprehensive-smoketest");
				throw;
			}
		}
	}
}
