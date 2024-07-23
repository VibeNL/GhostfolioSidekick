using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Model;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.EntityFrameworkCore;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class AutomatedStockSplitTask(IRestClient restCallClient) : IScheduledWork
	{
		private readonly object FinancialModelingPrepApiKey = "";

		public TaskPriority Priority => TaskPriority.AutomatedStockSplit;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public async Task DoWork()
		{
			var dbContext = await DatabaseContext.GetDatabaseContext();

			foreach (var item in dbContext.SymbolProfiles.Where(x => x.AssetSubClass == Database.Model.AssetSubClass.Stock))
			{
				if (item.StockSplitList != null)
				{
					continue;
				}

				// url https://financialmodelingprep.com/api/v3/historical-price-full/stock_split/NVDA?apikey=
				var url = $"https://financialmodelingprep.com/api/v3/historical-price-full/stock_split/{item.Symbol}?apikey={FinancialModelingPrepApiKey}";
				var restRequest = new RestRequest(url, Method.Get);
				var result = await restCallClient.GetAsync<SplitResult>(restRequest);

				if (result != null)
				{
					await dbContext.StockSplitLists.AddAsync(new StockSplitList
					{
						StockSplits = result.Historical.Select(x => new StockSplit
						{
							Date = x.Date,
							ToAmount = x.Numerator,
							FromAmount = x.Denominator,
							SymbolProfileId = item.Id
						}).ToList(),
						SymbolProfile = item,
						SymbolProfileId = item.Id
					});
				}
			}

		}

		public class SplitResult
		{
			public required string Symbol { get; set; }
			public required List<Split> Historical { get; set; }
		}

		public class Split
		{
			public DateOnly Date { get; set; }

			public required int Numerator { get; set; }

			public required int Denominator { get; set; }
		}
	}
}
