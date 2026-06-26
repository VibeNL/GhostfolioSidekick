using PortfolioViewer.WASM.UITests.PageObjects;
using xRetry.v3;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class TransactionsTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
{
	[RetryFact]
	public async Task ComprehensiveSmokeTest_LoginSyncAndViewTransactions()
	{
		await SetupAsync();

		var transactionsPage = new TransactionsPage(Page!);
		await transactionsPage.NavigateViaMenuAsync();
		await transactionsPage.WaitForPageLoadAsync();
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
}
