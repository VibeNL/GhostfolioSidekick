using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects
{
	public class HomePage(IPage page) : BasePageObject(page)
	{
		private const string SyncButtonSelector = "button.btn-primary:has-text('Sync')";
		private const string ProgressBarSelector = ".progress-bar";
		private const string CurrentActionSelector = "p:near(.progress)";
		private const string LastSyncTimeSelector = ".alert-info";
		private const string NoSyncWarningSelector = ".alert-warning";
		private const string OptionsDropdownSelector = "button.dropdown-toggle:has-text('Options')";
		private const string ForceFullSyncSelector = "button.dropdown-item:has-text('Force Full Sync')";
		private const string DeleteAllDataSelector = "button.dropdown-item:has-text('Delete All Data')";

		public async Task WaitForPageLoadAsync(int timeout = 10000, CancellationToken ct = default)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				// Wait for either the sync button or the dashboard heading
				await _page.WaitForSelectorAsync("h1:has-text('Dashboard')", new PageWaitForSelectorOptions { Timeout = timeout });
				await _page.WaitForSelectorAsync(SyncButtonSelector, new PageWaitForSelectorOptions { Timeout = timeout });
			}, ct);
		}

		public async Task<bool> IsSyncButtonVisibleAsync()
		{
			try
			{
				var button = await _page.QuerySelectorAsync(SyncButtonSelector);
				return button != null && await button.IsVisibleAsync();
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> IsSyncButtonEnabledAsync()
		{
			try
			{
				var button = await _page.QuerySelectorAsync(SyncButtonSelector);
				if (button == null) return false;
				
				var isDisabled = await button.GetAttributeAsync("disabled");
				return isDisabled == null;
			}
			catch
			{
				return false;
			}
		}

		public async Task ClickSyncButtonAsync()
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.ClickAsync(SyncButtonSelector);
					// Wait for sync to start (progress bar appears)
					await _page.WaitForSelectorAsync(ProgressBarSelector, new PageWaitForSelectorOptions { Timeout = 30000 });
			});
		}

		public async Task ClickSyncAndWaitForCompletionAsync(int timeoutMs = 120000, CancellationToken ct = default)
		{
			var syncButton = await _page.QuerySelectorAsync(SyncButtonSelector);
			if (syncButton == null) return;

			await syncButton.ClickAsync();

			// Wait for sync button to become enabled again (sync completed).
			// Wrapped in WaitAsync(ct) so a stuck sync aborts via the test's
			// cancellation token instead of always blocking for the full Playwright timeout.
			await _page.WaitForSelectorAsync(SyncButtonSelector + ":not([disabled])", new PageWaitForSelectorOptions { Timeout = timeoutMs })
				.WaitAsync(ct);
		}

		public async Task<string> GetSyncButtonTextAsync()
		{
			var button = await _page.QuerySelectorAsync(SyncButtonSelector);
			return button != null ? await button.TextContentAsync() ?? string.Empty : string.Empty;
		}

		public async Task<bool> IsSyncInProgressAsync()
		{
			var buttonText = await GetSyncButtonTextAsync();
			return buttonText.Contains("Syncing", StringComparison.OrdinalIgnoreCase) || 
			       !await IsSyncButtonEnabledAsync();
		}

		public async Task<int> GetProgressPercentageAsync()
		{
			try
			{
				var progressBar = await _page.QuerySelectorAsync(ProgressBarSelector);
				if (progressBar == null) return 0;

				var ariaValueNow = await progressBar.GetAttributeAsync("aria-valuenow");
				return int.TryParse(ariaValueNow, out var progress) ? progress : 0;
			}
			catch
			{
				return 0;
			}
		}

		public async Task<string> GetCurrentActionAsync()
		{
			try
			{
				var actionElement = await _page.QuerySelectorAsync(CurrentActionSelector);
				return actionElement != null ? await actionElement.TextContentAsync() ?? string.Empty : string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		public async Task<bool> HasLastSyncTimeAsync()
		{
			try
			{
				// After sync, last sync time is shown inline as text-muted "Last updated: ..."
				var element = await _page.QuerySelectorAsync("small.text-muted:has-text('Last updated')");
				if (element != null && await element.IsVisibleAsync())
					return true;

				// Fallback: also check for .alert-info (legacy/alternative rendering)
				element = await _page.QuerySelectorAsync(LastSyncTimeSelector);
				return element != null && await element.IsVisibleAsync();
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> HasNoSyncWarningAsync()
		{
			try
			{
				var element = await _page.QuerySelectorAsync(NoSyncWarningSelector);
				return element != null;
			}
			catch
			{
				return false;
			}
		}

		public async Task WaitForSyncToCompleteAsync(int timeoutInMilliseconds = 60000)
		{
			// Wait for the progress to reach 100% or the button to become enabled again
			var startTime = DateTime.Now;
			
			while ((DateTime.Now - startTime).TotalMilliseconds < timeoutInMilliseconds)
			{
				var progress = await GetProgressPercentageAsync();
				var isEnabled = await IsSyncButtonEnabledAsync();
				
				if (progress == 100 || isEnabled)
				{
						// Wait for sync button to become enabled again (sync completed)
						await _page.WaitForSelectorAsync(SyncButtonSelector, new PageWaitForSelectorOptions { Timeout = 5000 });
					return;
				}
				
					await _page.WaitForSelectorAsync(SyncButtonSelector, new PageWaitForSelectorOptions { Timeout = 2000 });
			}
			
			throw new TimeoutException($"Sync did not complete within {timeoutInMilliseconds}ms");
		}

		public async Task OpenOptionsMenuAsync()
		{
			await _page.ClickAsync(OptionsDropdownSelector);
			await _page.WaitForSelectorAsync(ForceFullSyncSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
		}

		public async Task ClickForceFullSyncAsync()
		{
			await OpenOptionsMenuAsync();
			await _page.ClickAsync(ForceFullSyncSelector);
		}

		public async Task ClickDeleteAllDataAsync()
		{
			await OpenOptionsMenuAsync();
			await _page.ClickAsync(DeleteAllDataSelector);
		}

		/// <summary>
		/// Measures horizontal overflow at the current viewport: window.innerWidth and documentElement.scrollWidth.
		/// scrollWidth greater than innerWidth means the page scrolls horizontally, i.e. content is clipped off-screen on mobile.
		/// </summary>
		public async Task<(int InnerWidth, int DocumentScrollWidth)> GetHorizontalOverflowMetricsAsync()
		{
			var innerWidth = await _page.EvaluateAsync<int>("() => window.innerWidth");
			var documentScrollWidth = await _page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
			return (innerWidth, documentScrollWidth);
		}

		/// <summary>
		/// Returns the computed white-space of a table header cell and body cell at the current viewport.
		/// A temporary probe element is used so this works even when no data rows are rendered
		/// (empty/error dashboard state) — it still guards the global .table td/th CSS rules.
		/// </summary>
		public async Task<string[]> GetComputedTableCellWhiteSpacesAsync()
		{
			const string script = "() => { const probe = document.createElement('div'); " +
				"probe.className = 'table-responsive'; " +
				"probe.innerHTML = '<table class=\"table\"><tbody><tr><th>label</th><td>value</td></tr></tbody></table>'; " +
				"document.body.appendChild(probe); " +
				"try { return [getComputedStyle(probe.querySelector('th')).whiteSpace, getComputedStyle(probe.querySelector('td')).whiteSpace]; } " +
				"finally { probe.remove(); } }";
			return await _page.EvaluateAsync<string[]>(script);
		}

		/// <summary>
		/// Waits up to <paramref name="timeout"/> ms for dashboard tables (.table-responsive) to render.
		/// Returns true when at least one table wrapper is present, false for the empty/error state.
		/// </summary>
		public async Task<bool> WaitForDashboardTablesAsync(int timeout = 10000, CancellationToken ct = default)
		{
			try
			{
				await _page.WaitForSelectorAsync(".table-responsive", new PageWaitForSelectorOptions { Timeout = timeout }).WaitAsync(ct);
				return true;
			}
			catch (TimeoutException)
			{
				return false; // Empty/error dashboard state — no tables rendered.
			}
		}

		/// <summary>
		/// Diagnoses document-level horizontal overflow by hide-and-measure descent: recursively finds the deepest DOM
		/// elements whose removal brings documentElement.scrollWidth back within the viewport. Catches fixed-positioned
		/// and otherwise invisible-to-bounding-box contributors that a plain rect scan misses.
		/// </summary>
		public async Task<string[]> GetOverflowingElementsAsync()
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
			return await _page.EvaluateAsync<string[]>(script);
		}

		/// <summary>
		/// Returns per-.table-responsive overflow in px (scrollWidth - clientWidth).
		/// Positive values mean the table content is wider than its wrapper and gets clipped or scrolls inside the card.
		/// </summary>
		public async Task<int[]> GetTableWrapperOverflowsAsync()
		{
			var wrappers = await _page.QuerySelectorAllAsync(".table-responsive");
			var overflows = new int[wrappers.Count];
			for (var i = 0; i < wrappers.Count; i++)
			{
				overflows[i] = await wrappers[i].EvaluateAsync<int>("el => el.scrollWidth - el.clientWidth");
			}
			return overflows;
		}
	}
}
