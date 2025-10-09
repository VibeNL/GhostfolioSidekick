using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class ServerConfigurationService(HttpClient httpClient) : IServerConfigurationService
	{
		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};

		private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		private Currency? _cachedPrimaryCurrency;
		private Task<Currency>? _primaryCurrencyTask;
		private readonly Lock _lock = new();

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
					_primaryCurrencyTask ??= FetchPrimaryCurrencyAsync();
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
					var result = JsonSerializer.Deserialize<PrimaryCurrencyResponse>(content, _jsonOptions);

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

		private sealed record PrimaryCurrencyResponse(string? PrimaryCurrency)
		{
		}
	}
}
