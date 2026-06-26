namespace PortfolioViewer.WASM.UITests
{
	[CollectionDefinition("WebApplicationFactory", DisableParallelization = true)]
	public class WebApplicationFactoryCollection : ICollectionFixture<CustomWebApplicationFactory>
	{
		// Prevents parallel execution across test classes sharing this collection.
		// Critical for in-memory SQLite shared-cache safety.
	}
}
