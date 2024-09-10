using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using Microsoft.Extensions.Logging;
using OoplesFinance.YahooFinanceAPI;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public class CurrencyRepository : ICurrencyRepository
	{
		private readonly Policy policy;
		private readonly ILogger<StockPriceRepository> logger;

		public CurrencyRepository(ILogger<StockPriceRepository> logger)
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
			var yahooClient = new YahooClient();

			var symbol = currencyFrom.Symbol + currencyTo.Symbol+"=X";

			try
			{

				var r = await policy.Execute(() => yahooClient.GetHistoricalDataAsync(symbol, OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily, fromDate.ToDateTime(TimeOnly.MinValue)));
				return r.Select(x => new MarketData(new Money(currencyTo, (decimal)x.Close), x.Date));
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to get currency history");
				return [];
			}
		}
	}
}
