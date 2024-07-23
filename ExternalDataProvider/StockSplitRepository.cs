using GhostfolioSidekick.Model.Market;
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

	public class StockSplitRepository : IStockSplitRepository
	{
		private Policy policy;
		private readonly ILogger<StockSplitRepository> logger;

		public StockSplitRepository(ILogger<StockSplitRepository> logger)
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

		public async Task<IEnumerable<StockSplit>> GetStockSplits(string symbol)
		{
			var yahooClient = new YahooClient();

			try
			{
				var r = policy.Execute(() => yahooClient.GetStockSplitDataAsync(symbol, OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily, new DateTime(1000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).GetAwaiter().GetResult());
				return r.Select(r => new StockSplit
				{
					Date = r.Date,
					FromFactor = GetSplit(r, false),
					ToFactor = GetSplit(r, true),
				});
			}
			catch (Exception e)
			{
				logger.LogWarning($"Failed to get stock split information for {symbol}");
				return [];
			}
		}

		private static int GetSplit(StockSplitData r, bool to)
		{
			var index = to ? 0 : 1;

			return int.Parse(r.StockSplit.Split(':')[index].Replace(".", string.Empty), CultureInfo.InvariantCulture);
		}
	}
}
