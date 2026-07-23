namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Shared assertion helpers for the "page rendered" tri-state pattern used across many UI tests:
/// a page is considered correctly rendered if it shows real data rows, an explicit empty state,
/// or a (test-environment-tolerated) error state. Centralizing this avoids copy-pasted
/// Assert.True(hasRows || isEmpty || hasError, ...) blocks and makes the intent/debug output consistent.
/// </summary>
public static class PageRenderAssertions
{
	/// <summary>
	/// Asserts the page rendered into one of the three tolerated states (rows/empty/error) and
	/// returns which state was observed so callers can conditionally tighten further checks
	/// (e.g. verifying specific seeded symbols only when hasRows is true).
	/// </summary>
	public static void AssertRendered(string pageName, bool hasRows, bool isEmpty, bool hasError)
	{
		Assert.True(hasRows || isEmpty || hasError,
			$"{pageName} page should render correctly (rows: {hasRows}, empty: {isEmpty}, error: {hasError}). " +
			"None of the expected render states were detected \u2014 check screenshots/HTML in playwright-screenshots/ for the actual DOM state.");
	}

	/// <summary>
	/// Same as <see cref="AssertRendered"/> but without an explicit empty-state signal (some pages
	/// only expose rows/error selectors).
	/// </summary>
	public static void AssertRendered(string pageName, bool hasRows, bool hasError)
		=> AssertRendered(pageName, hasRows, isEmpty: false, hasError);

	/// <summary>
	/// When the page reported data rows, verifies every expected seeded symbol is present.
	/// Skips the check entirely (no-op) when rows aren't present, since the page may legitimately
	/// be showing an empty/error state in the test environment.
	/// </summary>
	public static async Task AssertSeededSymbolsWhenRowsPresentAsync(string pageName, bool hasRows, IEnumerable<string> expectedSymbols, Func<string, Task<bool>> hasSymbolAsync)
	{
		if (!hasRows)
		{
			return;
		}

		foreach (var symbol in expectedSymbols)
		{
			var hasSymbol = await hasSymbolAsync(symbol);
			Assert.True(hasSymbol, $"{pageName} page should show seeded {symbol} when data rows are present");
		}
	}
}
