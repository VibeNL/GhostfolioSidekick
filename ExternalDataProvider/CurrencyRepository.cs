using GhostfolioSidekick.ExternalDataProvider.PolygonIO;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using Microsoft.Extensions.Logging;
using Polly;
using System.IO;
using System.Net.Http.Json;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public class CurrencyRepository : ICurrencyRepository
	{
		private readonly Policy policy;
		private readonly ILogger<CurrencyRepository> logger;

		static HttpClient client = new HttpClient();

		public CurrencyRepository(ILogger<CurrencyRepository> logger)
		{
			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetry(5, x => TimeSpan.FromSeconds(60), (exception, timeSpan, retryCount, context) =>
				{
					logger.LogWarning("The request failed");
				});

			policy = retryPolicy;
			this.logger = logger;
		}

		public async Task<IEnumerable<MarketData>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			try
			{
				HttpResponseMessage response = await client.GetAsync("https://api.polygon.io/v2/aggs/ticker/C:EURUSD/range/1/day/2023-01-09/2023-01-09?apiKey=");
				response.EnsureSuccessStatusCode();

				var r = await response.Content.ReadFromJsonAsync<CurrencyQueryResult>();

				return [];
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to get currency history");
				return [];
			}
		}
	}
}
