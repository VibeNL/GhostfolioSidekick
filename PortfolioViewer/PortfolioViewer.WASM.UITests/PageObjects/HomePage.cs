using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects
{
	public class HomePage(IPage page)
	{
		private readonly IPage _page = page;

		private const string SyncButtonSelector = "button.btn-primary:has-text('Sync')";
		private const string ProgressBarSelector = ".progress-bar";
		private const string CurrentActionSelector = "p:near(.progress)";
		private const string LastSyncTimeSelector = ".alert-info";
		private const string NoSyncWarningSelector = ".alert-warning";
		private const string OptionsDropdownSelector = "button.dropdown-toggle:has-text('Options')";
		private const string ForceFullSyncSelector = "button.dropdown-item:has-text('Force Full Sync')";
		private const string DeleteAllDataSelector = "button.dropdown-item:has-text('Delete All Data')";

		public async Task WaitForPageLoadAsync(int timeout = 10000)
		{
			// Wait for either the sync button or the start page heading
			await _page.WaitForSelectorAsync("h1:has-text('Start page')", new PageWaitForSelectorOptions { Timeout = timeout });
			await _page.WaitForSelectorAsync(SyncButtonSelector, new PageWaitForSelectorOptions { Timeout = timeout });
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
			await _page.ClickAsync(SyncButtonSelector);
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
				var element = await _page.QuerySelectorAsync(LastSyncTimeSelector);
				return element != null;
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

		public async Task WaitForSyncToCompleteAsync(int timeout = 60000)
		{
			// Wait for the progress to reach 100% or the button to become enabled again
			var startTime = DateTime.Now;
			
			while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
			{
				var progress = await GetProgressPercentageAsync();
				var isEnabled = await IsSyncButtonEnabledAsync();
				
				if (progress == 100 || isEnabled)
				{
					// Give it a bit more time to complete UI updates
					await _page.WaitForTimeoutAsync(1000);
					return;
				}
				
				await _page.WaitForTimeoutAsync(500);
			}
			
			throw new TimeoutException($"Sync did not complete within {timeout}ms");
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

		public async Task<string> TakeScreenshotAsync(string path)
		{
			await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
			return path;
		}
	}
}
