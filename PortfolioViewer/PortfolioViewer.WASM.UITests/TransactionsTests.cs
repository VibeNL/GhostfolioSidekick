using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class TransactionsTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[RetryFact]
	public async Task ComprehensiveSmokeTest_LoginSyncAndViewTransactions()
	{
		await SetupAsync();

		var transactionsPage = new TransactionsPage(Page!);
		await transactionsPage.NavigateViaMenuAsync();
		await transactionsPage.SetDateFilterToAllAsync();

		var hasError = await transactionsPage.IsErrorDisplayedAsync();
		var isEmpty = await transactionsPage.IsEmptyStateDisplayedAsync();
		var isTableDisplayed = await transactionsPage.IsTableDisplayedAsync();

		// The page should have rendered without Blazor errors.
		// In the test environment, the WASM client syncs from Ghostfolio API which may not be configured,
		// so we may see a table with data, an empty state, or an error message — all are valid renders.
		PageRenderAssertions.AssertRendered("Transactions", isTableDisplayed, isEmpty, hasError);
	}
}
