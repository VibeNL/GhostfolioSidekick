using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class AuthTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[RetryFact]
	public async Task Api_HealthEndpoint_GivesResponse()
	{
		var apiClient = Fixture.CreateDefaultClient();
		var healthUrl = "api/auth/health";

		var response = await apiClient.GetAsync(healthUrl, CancellationToken);
		var content = await response.Content.ReadAsStringAsync(CancellationToken);
		Assert.True(response.IsSuccessStatusCode, $"API health endpoint failed: {response.StatusCode} {content}");
	}

	[RetryFact]
	public async Task Login_ShouldSucceedWithValidToken()
	{
		await SetupAsync();

		Assert.False(LoginPage.IsOnLoginPage(), "Should not be on login page after successful login");
	}
}
