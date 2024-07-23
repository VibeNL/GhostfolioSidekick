using GhostfolioSidekick.Model.Market;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider
{

	public class StockSplitRepository : IStockSplitRepository
	{
		public async Task<IEnumerable<StockSplit>> GetStockSplits(string symbol, DateTime startDate)
		{
			var yahooClient = new YahooClient();

			var r = await yahooClient.GetStockSplitDataAsync(symbol, OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily, startDate);

			return r.Select(r => new StockSplit
			{
				Date = r.Date,
				FromFactor = GetSplit(r, false),
				ToFactor = GetSplit(r, true),
			});
		}

		private static int GetSplit(StockSplitData r, bool to)
		{
			var index = to ? 0 : 1;

			return int.Parse(r.StockSplit.Split(':')[index]);		
		}
	}
}
