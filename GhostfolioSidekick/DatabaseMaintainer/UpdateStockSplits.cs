//using GhostfolioSidekick.Database.Model;
//using GhostfolioSidekick.Database.Repository;
//using GhostfolioSidekick.ExternalDataProvider;
//using GhostfolioSidekick.GhostfolioAPI.API;
//using GhostfolioSidekick.Model.Activities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Polly.Caching;
//using RestSharp;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GhostfolioSidekick.DatabaseMaintainer
//{
//	internal partial class UpdateStockSplits(
//		IStockSplitRepository stockSplitRepository, 
//		IMarketDataRepository marketDataRepository,
//		ITaskRunsRepository taskRunsRepository,
//		ILogger logger)
//	{
//		public async Task DoWork()
//		{
//			if (taskRunsRepository.GetLastTaskRun(TypeOfTaskRun.StockSplit).LastUpdate >= DateTime.Today)
//			{
//				return;
//			}

//			var dateTime = DateTime.Now;

//			foreach (var item in marketDataRepository.GetSymbols().Where(x => x.AssetSubClass == Model.Activities.AssetSubClass.Stock))
//			{
				
//				try
//				{
//					var r = await stockSplitRepository.GetStockSplits(item);

//					var splits = r.Select(r => new StockSplit
//					{
//						Date = DateOnly.FromDateTime(r.Date),
//						FromAmount = r.FromFactor,
//						ToAmount = r.ToFactor,
//					}).ToList();

//					item.StockSplitList = new StockSplitList { SymbolProfile = item, SymbolProfileId = item.Id, StockSplits = splits, LastUpdate = dateTime };
//					logger.LogDebug("Got stock splits for {symbol}", item.Symbol);
//					await dbContext.SaveChangesAsync();
//				}
//				catch
//				{
//					logger.LogWarning("Failed to get stock splits for {symbol}", item.Symbol);
//				}
//			}
//		}
//	}
//}
