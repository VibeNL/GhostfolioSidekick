using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class AuthTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
    [RetryFact]
    public async Task Api_HealthEndpoint_GivesResponse()
    {
        var apiClient = Fixture.CreateDefaultClient();
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

    [RetryFact]
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
}
