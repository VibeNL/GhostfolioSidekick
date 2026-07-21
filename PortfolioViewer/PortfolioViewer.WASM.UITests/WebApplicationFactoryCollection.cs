using Xunit;

namespace PortfolioViewer.WASM.UITests
{
	[CollectionDefinition("WebApplicationFactory", DisableParallelization = true)]
	public class WebApplicationFactoryCollection : ICollectionFixture<CustomWebApplicationFactory>, ICollectionFixture<BrowserFixture>
	{
		// Prevents parallel execution across test classes sharing this collection.
		// Critical for in-memory SQLite shared-cache safety.
		// BrowserFixture provides a shared Chromium instance for the entire run.

		static WebApplicationFactoryCollection()
		{
			// Clean old Playwright artifacts before each test run.
			PlaywrightArtifactsCleanup.Cleanup();
		}
	}
}
