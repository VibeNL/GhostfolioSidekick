using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class TransactionsTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
    [RetryFact]
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

            var isSyncInProgress = await homePage.IsSyncInProgressAsync();
            Assert.True(isSyncInProgress, "Sync should be in progress");

            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

            var hasLastSyncTime = await homePage.HasLastSyncTimeAsync();
            Assert.True(hasLastSyncTime, "Last sync time should be displayed after sync");

            await transactionsPage.NavigateViaMenuAsync();
            await transactionsPage.WaitForPageLoadAsync(timeout: 30000);
            await transactionsPage.SetDateFilterToAllAsync();

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
