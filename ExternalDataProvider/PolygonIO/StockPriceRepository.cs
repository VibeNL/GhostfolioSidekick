using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.ExternalDataProvider.PolygonIO.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Polly;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO
{
	public class StockPriceRepository : IStockPriceRepository
	{
		private readonly Policy policy;
		private readonly ILogger<StockPriceRepository> logger;
		private readonly string apiKey;

		static HttpClient client = new HttpClient();

		public StockPriceRepository(ILogger<StockPriceRepository> logger, IApplicationSettings applicationSettings)
		{
			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetry(5, x => TimeSpan.FromSeconds(60), (exception, timeSpan, retryCount, context) =>
				{
					logger.LogWarning("The request failed");
				});

			this.policy = retryPolicy;
			this.logger = logger;
			this.apiKey = applicationSettings.ConfigurationInstance?.Settings?.DataProviderPolygonIOApiKey;
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
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

				string requestUri = $"https://api.polygon.io/v2/aggs/ticker/{symbol.Symbol.ToUpperInvariant()}/range/1/day/{fromDate:yyyy-MM-dd}/{DateTime.Today:yyyy-MM-dd}?apiKey={apiKey}";
				HttpResponseMessage response = await client.GetAsync(requestUri);
				response.EnsureSuccessStatusCode();

				var r = await response.Content.ReadFromJsonAsync<TickerQueryResult>() ?? throw new NotSupportedException();

				return r.Results.Select(x => new MarketData(
					new Money(symbol.Currency, x.C),
					new Money(symbol.Currency, x.O),
					new Money(symbol.Currency, x.H),
					new Money(symbol.Currency, x.L),
					x.V,
					x.N,
					new Money(symbol.Currency, x.VW),
					UnitTimeStamp.UnixTimeStampToDateTime(x.T)));
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to get stock price history");
				return [];
			}
		}
	}
}
