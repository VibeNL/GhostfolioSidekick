using PortfolioViewer.WASM.UITests.PageObjects;
using GhostfolioSidekick.Tools.TestUtilities;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class AuthTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[Fact]
	public async Task Api_HealthEndpoint_GivesResponse()
	{
		await TestRetry.RunAsync(Api_HealthEndpoint_GivesResponse_Runnable);
	}

	private async Task Api_HealthEndpoint_GivesResponse_Runnable()
	{
		var apiClient = Fixture.CreateDefaultClient();
		var healthUrl = "api/auth/health";

		var response = await apiClient.GetAsync(healthUrl, CancellationToken);
		var content = await response.Content.ReadAsStringAsync(CancellationToken);
		Assert.True(response.IsSuccessStatusCode, $"API health endpoint failed: {response.StatusCode} {content}");
	}

	[Fact]
	public async Task Login_ShouldSucceedWithValidToken()
	{
		await TestRetry.RunAsync(Login_ShouldSucceedWithValidToken_Runnable);
	}

	private async Task Login_ShouldSucceedWithValidToken_Runnable()
	{
		await SetupAsync();

		Assert.False(LoginPage.IsOnLoginPage(), "Should not be on login page after successful login");
	}
}
