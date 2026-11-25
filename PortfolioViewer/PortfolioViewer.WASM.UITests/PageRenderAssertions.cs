using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Shared page-render helpers used across UI tests: the "page rendered" tri-state assertions (a page is
/// correctly rendered if it shows real data rows, an explicit empty state, or a test-environment-tolerated
/// error state), the standard stable-state page-load wait (<see cref="WaitForStableStateAsync"/>), and the
/// document-level horizontal-overflow checks. Centralizing these avoids copy-pasted assertion/wait blocks
/// and keeps intent/debug output consistent across test classes.
/// </summary>
public static class PageRenderAssertions
{
	/// <summary>
	/// Asserts the page rendered into one of the three tolerated states (rows/empty/error).
	/// Callers can use their own <paramref name="hasRows"/> flag to conditionally tighten further checks
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

	/// <summary>
	/// Standard page-load wait shared by all page objects and the viewport smoke tests: waits until any of the given
	/// stable-state selectors is visible, then confirms the loading spinner has disappeared (pages without a spinner
	/// pass instantly). Checking the spinner after content appeared guarantees anything measured afterwards reflects a
	/// fully loaded page. Cancellation propagates; per-selector timeouts just try the next selector.
	/// </summary>
	public static async Task WaitForStableStateAsync(IPage page, IEnumerable<string> stableStateSelectors, int timeout = 20_000, CancellationToken ct = default)
	{
		var selectors = stableStateSelectors.ToArray();

		// Wait for any stable state. Single shared deadline so the per-selector attempts split the budget
		// instead of each consuming the full timeout.
		var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
		var matched = false;
		foreach (var selector in selectors)
		{
			try
			{
				await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = Math.Max(1, (long)(deadline - DateTime.UtcNow).TotalMilliseconds) })
					.WaitAsync(ct);
				matched = true;
				break;
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				// Try next selector
			}
		}

		if (!matched)
		{
			throw new TimeoutException($"WaitForStableStateAsync: none of the stable state selectors matched within {timeout}ms. Tried: {string.Join(", ", selectors)}");
		}

		// Wait for loading spinner to disappear (optional — may not exist). Swallowed on timeout, as before.
		try
		{
			await page.WaitForSelectorAsync(".spinner-border", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden, Timeout = Math.Min(5000, timeout / 2) })
				.WaitAsync(ct);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			// No spinner or already hidden — continue
		}
	}

	/// <summary>
	/// Returns the elements causing horizontal overflow via hide-and-measure descent (plain rect scans miss
	/// inline-content overflow). Returns ["no-overflow"] when the document fits within the viewport (+2px tolerance).
	/// </summary>
	public static async Task<string[]> GetOverflowingElementsAsync(IPage page)
	{
		const string script = "() => { " +
			"const vw = window.innerWidth; const TOL = 2; " +
			"const sw = () => document.documentElement.scrollWidth; " +
			"if (sw() <= vw + TOL) return ['no-overflow']; " +
			"const out = []; " +
			"function describe(el) { const r = el.getBoundingClientRect(); " +
			"const cls = typeof el.className === 'string' ? '.' + el.className.trim().split(/\\s+/).slice(0, 3).join('.') : ''; " +
			"return el.tagName.toLowerCase() + cls + ' [left=' + Math.round(r.left) + ', right=' + Math.round(r.right) + ', w=' + Math.round(r.width) + ']'; } " +
			"function descend(el, depth) { " +
			"if (depth > 15 || el.children.length === 0) { out.push(describe(el)); return; } " +
			"let anyChildCauses = false; " +
			"for (const child of el.children) { " +
			"const prev = child.style.display; " +
			"child.style.display = 'none'; " +
			"const removed = sw() <= vw + TOL; " +
			"child.style.display = prev; " +
			"if (removed) { anyChildCauses = true; descend(child, depth + 1); } " +
			"} " +
			"if (!anyChildCauses) out.push(describe(el)); " +
			"} " +
			"descend(document.body, 0); " +
			"return out.slice(0, 20); }";
		return await page.EvaluateAsync<string[]>(script);
	}

	/// <summary>
	/// Asserts the current page has no document-level horizontal overflow: the document fits within the viewport,
	/// so nothing is clipped off-screen and the page itself never gets a horizontal scrollbar.
	/// Tables wider than their .table-responsive wrapper are NOT failures — inner scrolling inside the card is
	/// Bootstrap's standard pattern for wide tables (the app-wide convention; only Home/Accounts hide columns instead).
	/// Pass a context string identifying route + viewport; failure artifacts (screenshot/HTML/console) land in playwright-screenshots/.
	/// </summary>
	public static async Task AssertNoHorizontalOverflowAsync(IPage page, string context)
	{
		const int tolerancePx = 2; // Sub-pixel rounding tolerance for layout metrics

		var innerWidth = await page.EvaluateAsync<int>("() => window.innerWidth");
		var documentScrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
		string[] overflowingElements = [];
		if (documentScrollWidth > innerWidth + tolerancePx)
		{
			overflowingElements = await GetOverflowingElementsAsync(page);
		}

		Assert.True(documentScrollWidth <= innerWidth + tolerancePx,
			$"{context}: document scrollWidth is {documentScrollWidth}px but the viewport is only {innerWidth}px wide — content wider than the viewport gets clipped off-screen. Offending elements: [{string.Join("; ", overflowingElements)}]. Check playwright-screenshots/ for the actual layout.");
	}
}
