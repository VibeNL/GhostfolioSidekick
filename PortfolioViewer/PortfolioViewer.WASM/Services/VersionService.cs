using GhostfolioSidekick.PortfolioViewer.Common;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class VersionService : IVersionService
	{
		private readonly HttpClient _httpClient;
		private string? _cachedServerVersion;

		public VersionService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public string ClientVersion => VersionInfo.Version;

		public async Task<string?> GetServerVersionAsync()
		{
			try
			{
				var response = await _httpClient.GetFromJsonAsync<VersionResponse>("/api/version");
				_cachedServerVersion = response?.Version;
				return _cachedServerVersion;
			}
			catch
			{
				return _cachedServerVersion;
			}
		}

		public async Task<bool> IsUpdateAvailableAsync()
		{
			var serverVersion = await GetServerVersionAsync();
			if (string.IsNullOrEmpty(serverVersion))
			{
				return false;
			}

			return serverVersion != ClientVersion;
		}

		private sealed class VersionResponse
		{
			public string Version { get; set; } = string.Empty;
		}
	}
}
