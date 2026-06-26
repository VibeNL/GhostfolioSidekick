using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class HomePageTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
	[RetryFact]
	public async Task Sync_ShouldStartAndComplete()
	{
		await SetupAsync();

		var isSyncButtonEnabled = await HomePage.IsSyncButtonEnabledAsync();
		Assert.True(isSyncButtonEnabled, "Sync button should be enabled after sync completes");

		var hasLastSyncTime = await HomePage.HasLastSyncTimeAsync();
		Assert.True(hasLastSyncTime, "Last sync time should be displayed after successful sync");
	}
}
