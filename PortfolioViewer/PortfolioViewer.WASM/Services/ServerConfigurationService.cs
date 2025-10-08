using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class ServerConfigurationService(HttpClient httpClient) : IServerConfigurationService
	{
		private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		private Currency? _cachedPrimaryCurrency;
		private Task<Currency>? _primaryCurrencyTask;
		private readonly object _lock = new();

		public Currency PrimaryCurrency
		{
			get
			{
				// Return cached value if available, otherwise return EUR as default
				return _cachedPrimaryCurrency ?? Currency.EUR;
			}
		}

		public Task<Currency> GetPrimaryCurrencyAsync()
		{
			// Double-checked locking pattern for thread safety
			if (_primaryCurrencyTask == null)
			{
				lock (_lock)
				{
					if (_primaryCurrencyTask == null)
					{
						_primaryCurrencyTask = FetchPrimaryCurrencyAsync();
					}
				}
			}

			return _primaryCurrencyTask;
		}

		private async Task<Currency> FetchPrimaryCurrencyAsync()
		{
			if (_cachedPrimaryCurrency != null)
			{
				return _cachedPrimaryCurrency;
			}

			try
			{
				var response = await _httpClient.GetAsync("api/configuration/primary-currency");
				if (response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					var result = JsonSerializer.Deserialize<PrimaryCurrencyResponse>(content, new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					});
					
					if (!string.IsNullOrWhiteSpace(result?.PrimaryCurrency))
					{
						_cachedPrimaryCurrency = Currency.GetCurrency(result.PrimaryCurrency);
						return _cachedPrimaryCurrency;
					}
				}
			}
			catch (Exception)
			{
				// Log the exception in a real-world scenario
				// For now, fall back to default
			}

			// Fallback to EUR if API call fails
			_cachedPrimaryCurrency = Currency.EUR;
			return _cachedPrimaryCurrency;
		}

		private class PrimaryCurrencyResponse
		{
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "<Pending>")]
			public string? PrimaryCurrency { get; set; }
		}
	}
}
