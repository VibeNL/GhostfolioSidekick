using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class SyncTrackingService : ISyncTrackingService
	{
		private const string LastSyncKey = "lastSyncTime";
		private readonly IJSRuntime _jsRuntime;

		public SyncTrackingService(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
		}

		public async Task<DateTime?> GetLastSyncTimeAsync()
		{
			try
			{
				var storedValue = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LastSyncKey);
				if (string.IsNullOrEmpty(storedValue))
				{
					return null;
				}

				if (DateTime.TryParse(storedValue, out var lastSyncTime))
				{
					return lastSyncTime;
				}

				return null;
			}
			catch
			{
				return null;
			}
		}

		public async Task SetLastSyncTimeAsync(DateTime syncTime)
		{
			try
			{
				var isoString = syncTime.ToString("O"); // ISO 8601 format
				await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LastSyncKey, isoString);
			}
			catch
			{
				// Ignore errors when storing
			}
		}

		public async Task<bool> HasEverSyncedAsync()
		{
			var lastSync = await GetLastSyncTimeAsync();
			return lastSync.HasValue;
		}

		public async Task ClearSyncTimeAsync()
		{
			try
			{
				await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", LastSyncKey);
			}
			catch
			{
				// Ignore errors when clearing
			}
		}
	}
}