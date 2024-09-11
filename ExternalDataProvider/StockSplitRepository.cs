//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Market;
//using GhostfolioSidekick.Model.Symbols;
//using Microsoft.Extensions.Logging;
//using OoplesFinance.YahooFinanceAPI;
//using OoplesFinance.YahooFinanceAPI.Models;
//using Polly;
//using System.Globalization;

//namespace GhostfolioSidekick.ExternalDataProvider
//{

//	public class StockSplitRepository : IStockSplitRepository
//	{
//		private readonly Policy policy;
//		private readonly ILogger<StockSplitRepository> logger;

//		public StockSplitRepository(ILogger<StockSplitRepository> logger)
//		{
//			var retryPolicy = Policy
//				.Handle<Exception>()
//				.WaitAndRetry(5, x => TimeSpan.FromSeconds(60), (exception, timeSpan, retryCount, context) =>
//				{
//					logger.LogWarning("The request failed");
//				});

//			policy = retryPolicy;
//			this.logger = logger;
//		}

//		public async Task<IEnumerable<StockSplit>> GetStockSplits(SymbolProfile symbol)
//		{
//			if (symbol?.AssetSubClass != AssetSubClass.Stock)
//			{
//				return [];
//			}

//			var yahooClient = new YahooClient();

//			var r = await policy.Execute(async () => await yahooClient.GetStockSplitDataAsync(symbol.Symbol, OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily, new DateTime(1000, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
//			return r.Select(r => new StockSplit
//			{
//				Date = r.Date,
//				FromFactor = GetSplit(r, false),
//				ToFactor = GetSplit(r, true),
//			});
//		}

//		private static int GetSplit(StockSplitData r, bool to)
//		{
//			var index = to ? 0 : 1;

//			return int.Parse(r.StockSplit.Split(':')[index].Replace(".", string.Empty), CultureInfo.InvariantCulture);
//		}
//	}
//}
