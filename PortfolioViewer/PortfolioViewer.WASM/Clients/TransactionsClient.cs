using static PortfolioViewer.WASM.Pages.Weather;
using System.Net.Http.Json;
using GhostfolioSidekick.Model.Accounts;

namespace PortfolioViewer.WASM.Clients
{
	public class TransactionsClient(HttpClient httpClient)
	{
		public async Task<Platform[]> GetWeatherAsync(
			int maxItems = 10,
			CancellationToken cancellationToken = default)
		{
			try
			{
				List<Platform>? forecasts = null;

				var url = httpClient.BaseAddress.ToString();
				var a = httpClient.Timeout;


				await foreach (var forecast in
					httpClient.GetFromJsonAsAsyncEnumerable<Platform>(
						"/weatherforecast", cancellationToken))
				{
					if (forecasts?.Count >= maxItems)
					{
						break;
					}
					if (forecast is not null)
					{
						forecasts ??= [];
						forecasts.Add(forecast);
					}
				}

				return forecasts?.ToArray() ?? [];
			}
			catch (Exception ex)
			{
				var error = ex.Message;
				Console.WriteLine(ex.Message);
				return [];
			}
		}
	}
}
