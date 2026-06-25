using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class HomePageTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
    [RetryFact]
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

            var isSyncInProgress = await homePage.IsSyncInProgressAsync();
            Assert.True(isSyncInProgress, "Sync should be in progress after clicking sync button");

            await homePage.WaitForSyncToCompleteAsync(timeoutInMilliseconds: 120000);

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
}
