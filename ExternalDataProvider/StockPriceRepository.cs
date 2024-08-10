using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Models;
using Polly;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider
{

	public class StockPriceRepository : IStockPriceRepository
	{
		private readonly Policy policy;
		private readonly ILogger<StockPriceRepository> logger;

		public StockPriceRepository(ILogger<StockPriceRepository> logger)
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

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol)
		{
			if (!(symbol?.AssetSubClass == AssetSubClass.Stock || symbol?.AssetSubClass != AssetSubClass.Etf))
			{
				return [];
			}

			var yahooClient = new YahooClient();

			var r = await policy.Execute(() => yahooClient.GetHistoricalDataAsync(symbol!.Symbol, OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily, new DateTime(1000, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
			return r.Select(x => new MarketData(new Money(symbol.Currency, (decimal)x.Close), x.Date));
		}
	}
}
