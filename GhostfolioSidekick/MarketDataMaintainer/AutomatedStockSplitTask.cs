using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Model;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.EntityFrameworkCore;
using Polly.Caching;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal partial class AutomatedStockSplitTask(IStockSplitRepository stockSplitRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.AutomatedStockSplit;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public async Task DoWork()
		{
			var dbContext = await DatabaseContext.GetDatabaseContext();

			foreach (var item in dbContext.SymbolProfiles.Include(x => x.StockSplitList).Where(x => x.AssetSubClass == Database.Model.AssetSubClass.Stock))
			{
				if (item.StockSplitList != null)
				{
					continue;
				}

				var r = await stockSplitRepository.GetStockSplits(item.Symbol, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));

				var splits = r.Select(r => new Database.Model.StockSplit
				{
					Date = DateOnly.FromDateTime(r.Date),
					FromAmount = r.FromFactor,
					ToAmount = r.ToFactor,
				}).ToList();

				item.StockSplitList = new StockSplitList { SymbolProfile = item, SymbolProfileId = item.Id, StockSplits = splits };
			}

		}
	}
}
