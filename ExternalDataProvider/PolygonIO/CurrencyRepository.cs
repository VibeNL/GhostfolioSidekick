using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.ExternalDataProvider.PolygonIO.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using Microsoft.Extensions.Logging;
using Polly;
using System.IO;
using System.Net.Http.Json;

namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO
{
	public class CurrencyRepository : ICurrencyRepository
	{
		private readonly Policy policy;
		private readonly ILogger<CurrencyRepository> logger;
		private readonly string apiKey;

		static HttpClient client = new HttpClient();

		public CurrencyRepository(ILogger<CurrencyRepository> logger, IApplicationSettings applicationSettings)
		{
			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetry(5, x => TimeSpan.FromSeconds(60), (exception, timeSpan, retryCount, context) =>
				{
					logger.LogWarning("The request failed");
				});

			policy = retryPolicy;
			this.logger = logger;
			this.apiKey = applicationSettings.ConfigurationInstance?.Settings?.DataProviderPolygonIOApiKey;
		}

		public async Task<IEnumerable<MarketData>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(apiKey))
				{
					return [];
				}

				if ((DateTime.Today - fromDate.ToDateTime(TimeOnly.MinValue)).TotalDays > (365 * 2))
				{
					fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-365 * 2));
				}

				string requestUri = $"https://api.polygon.io/v2/aggs/ticker/C:{currencyFrom.Symbol.ToUpperInvariant()}{currencyTo.Symbol.ToUpperInvariant()}/range/1/day/{fromDate:yyyy-MM-dd}/{DateTime.Today:yyyy-MM-dd}?apiKey={apiKey}";
				HttpResponseMessage response = await client.GetAsync(requestUri);
				response.EnsureSuccessStatusCode();

				var r = await response.Content.ReadFromJsonAsync<TickerQueryResult>() ?? throw new NotSupportedException();

				return r.Results.Select(x => new MarketData(
					new Money(currencyFrom, x.C),
					new Money(currencyFrom, x.O),
					new Money(currencyFrom, x.H),
					new Money(currencyFrom, x.L),
					x.V,
					x.N,
					new Money(currencyFrom, x.VW),
					UnitTimeStamp.UnixTimeStampToDateTime(x.T)));
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to get currency history");
				return [];
			}
		}
	}
}
