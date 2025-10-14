using Microsoft.JSInterop;
using System.Globalization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class SyncTrackingService(IJSRuntime jsRuntime) : ISyncTrackingService
	{
		private const string LastSyncKey = "lastSyncTime";

		public async Task<DateTime?> GetLastSyncTimeAsync()
		{
			try
			{
				var storedValue = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", LastSyncKey);
				if (string.IsNullOrEmpty(storedValue))
				{
					return null;
				}

				if (DateTime.TryParse(storedValue, CultureInfo.InvariantCulture, out var lastSyncTime))
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
				await jsRuntime.InvokeVoidAsync("localStorage.setItem", LastSyncKey, isoString);
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
				await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LastSyncKey);
			}
			catch
			{
				// Ignore errors when clearing
			}
		}
	}
}