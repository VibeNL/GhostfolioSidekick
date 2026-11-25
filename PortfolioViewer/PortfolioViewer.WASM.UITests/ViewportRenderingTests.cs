using GhostfolioSidekick.Tools.TestUtilities;

namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Cross-viewport rendering smoke tests: every top-level screen plus the deterministic parameterized screens must
/// render without horizontal overflow at phone, laptop and ultrawide widths. Catches the class of clipping bug where
/// content wider than the viewport gets cut off-screen (see Dashboard_ShouldNotClipTablesAtMobileViewport for the
/// original mobile regression).
/// </summary>
[Collection("WebApplicationFactory")]
public class ViewportRenderingTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	// Every top-level authenticated route (pre-login /login excluded) — keep in sync with the @page directives / NavMenu.
	private static readonly string[] AllRoutes =
	[
		"/", "/accounts", "/tax-report", "/holdings-price-targets", "/holdings", "/portfolio-timeseries",
		"/top-movers", "/transactions", "/dividends", "/task-status", "/data-issues",
		"/holding-identifier-mappings", "/tables", "/transaction-debug", "/price-targets"
	];

	// Deterministic parameterized routes — the symbols are seeded by TestDataSeeder so these resolve to real data.
	// (/accounts/{id} and /tax-report/account/{id}/{year} need a runtime account id and are covered in the Runnable.)
	private static readonly string[] ParameterizedRoutes = ["/holding/AAPL"];

	[Theory]
	[InlineData("phone", 390, 844)]
	[InlineData("laptop", 1440, 900)]
	[InlineData("ultrawide", 2560, 1080)]
	public async Task AllScreens_ShouldNotOverflowHorizontally(string name, int width, int height)
	{
		Assert.True(await TestRetry.RunAsync(() => AllScreens_ShouldNotOverflow_Runnable(name, width, height)), "Test failed after all retry attempts.");
	}

	private async Task AllScreens_ShouldNotOverflow_Runnable(string name, int width, int height)
	{
		// Set before login so the whole flow (login + sync + every route) renders at this width.
		await Page!.SetViewportSizeAsync(width, height);

		await SetupAsync();

		var context = $"{name} viewport {width}x{height}";
		string? accountDetailHref = null;
		foreach (var route in AllRoutes)
		{
			Console.WriteLine($"[Viewport] {context} → {route}");
			await AssertRouteRendersWithoutOverflowAsync(route, context);

			if (route == "/accounts")
			{
				// Best-effort detail-page coverage: drill into the first account row when data rows are present.
				accountDetailHref = await GetFirstElementAttributeAsync("a[href^='/accounts/']", "href");
				if (accountDetailHref != null)
				{
					if (name == "phone")
					{
						// Low-priority columns are hidden below md (Home convention) so Table 1 fits without inner scrolling.
						foreach (var column in new[] { "Asset Value", "Cash Balance", "Gain/Loss" })
						{
							var header = await Page!.QuerySelectorAsync($"th:text-is('{column}')");
							Assert.False(header != null && await header.IsVisibleAsync(), $"{context} /accounts: '{column}' column should be hidden at phone width.");
						}
					}

					Console.WriteLine($"[Viewport] {context} → {accountDetailHref} (account detail)");
					await AssertRouteRendersWithoutOverflowAsync(accountDetailHref, $"{context} (account detail)");
				}
				else
				{
					Console.WriteLine("[Viewport] No account rows rendered — skipping account detail page.");
				}
			}
		}

		foreach (var route in ParameterizedRoutes)
		{
			Console.WriteLine($"[Viewport] {context} → {route}");
			await AssertRouteRendersWithoutOverflowAsync(route, context);
		}

		if (accountDetailHref != null)
		{
			// /tax-report/account/{id}/{year}: account id comes from the seeded "Test Account" row; the seeder anchors data to UtcNow so the
			// current year is populated (the page renders fine even for a year without rows).
			var accountId = accountDetailHref.Split('/', 3)[2];
			var yearSummaryRoute = $"/tax-report/account/{accountId}/{DateTime.UtcNow.Year}";
			Console.WriteLine($"[Viewport] {context} → {yearSummaryRoute} (account year summary)");
			await AssertRouteRendersWithoutOverflowAsync(yearSummaryRoute, $"{context} (account year summary)");
		}
	}

	private async Task AssertRouteRendersWithoutOverflowAsync(string route, string context)
	{
		await Page!.GotoAsync(AbsoluteUrl(route));

		// nav.navbar is rendered by MainLayout on every page; some screens (e.g., /tables) only show controls in their initial state.
		await PageRenderAssertions.WaitForStableStateAsync(Page!, ["nav.navbar", ".table", ".card", ".alert-danger"], 20_000, CancellationToken);

		var errorElement = await Page.QuerySelectorAsync("#blazor-error-ui");
		if (errorElement != null && await errorElement.IsVisibleAsync())
		{
			throw new InvalidOperationException($"{context} {route}: Blazor framework error UI is visible — {(await errorElement.TextContentAsync() ?? string.Empty).Trim()}");
		}

		await PageRenderAssertions.AssertNoHorizontalOverflowAsync(Page!, $"{context} {route}");
	}

	private async Task<string?> GetFirstElementAttributeAsync(string selector, string attribute)
	{
		var element = await Page!.QuerySelectorAsync(selector);
		return element == null ? null : await element.GetAttributeAsync(attribute);
	}

	private string AbsoluteUrl(string path) => new Uri(new Uri(ServerAddress), path).ToString();
}
