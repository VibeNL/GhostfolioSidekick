using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Model;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly.Caching;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.DatabaseMaintainer
{
	internal partial class UpdateStockSplits(IStockSplitRepository stockSplitRepository, ILogger logger)
	{
		public async Task DoWork()
		{
			var dbContext = await DatabaseContext.GetDatabaseContext();
			var dateTime = DateTime.Now;

			foreach (var item in dbContext.SymbolProfiles.Include(x => x.StockSplitList).Where(x => x.AssetSubClass == AssetSubClass.Stock))
			{
				if (item.StockSplitList?.LastUpdate >= DateTime.Today)
				{
					continue;
				}

				try
				{
					var r = await stockSplitRepository.GetStockSplits(item);

					var splits = r.Select(r => new StockSplit
					{
						Date = DateOnly.FromDateTime(r.Date),
						FromAmount = r.FromFactor,
						ToAmount = r.ToFactor,
					}).ToList();

					item.StockSplitList = new StockSplitList { SymbolProfile = item, SymbolProfileId = item.Id, StockSplits = splits, LastUpdate = dateTime };
					logger.LogDebug("Got stock splits for {symbol}", item.Symbol);
					await dbContext.SaveChangesAsync();
				}
				catch
				{
					logger.LogWarning("Failed to get stock splits for {symbol}", item.Symbol);
				}
			}
		}
	}
}
