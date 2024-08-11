//using GhostfolioSidekick.Database;
//using GhostfolioSidekick.Database.Model;
//using GhostfolioSidekick.ExternalDataProvider;
//using GhostfolioSidekick.GhostfolioAPI.API;
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
//	internal partial class UpdateStockPrices(IStockPriceRepository stockPriceRepository, ILogger logger)
//	{
//		public async Task DoWork()
//		{
//			var dbContext = await DatabaseContext.GetDatabaseContext();
//			var dateTime = DateTime.Now;

//			foreach (var item in dbContext.SymbolProfiles.Include(x => x.StockPricesList))
//			{
//				if (item.StockPricesList?.LastUpdate >= DateTime.Today)
//				{
//					continue;
//				}

//				try
//				{
//					var r = await stockPriceRepository.GetStockMarketData(item);

//					if (r == null)
//					{
//						logger.LogWarning("Failed to get stock prices for {Symbol}", item.Symbol);
//						continue;
//					}

//					var prices = r.Select(r => new StockPrice
//					{
//						Date = DateOnly.FromDateTime(r.Date),
//						Currency = dbContext.Currencies.Find(r.MarketPrice.Currency.Symbol)!,
//						Close = (double)r.MarketPrice.Amount,
//					}).ToList();

//					item.StockPricesList = new StockPriceList { SymbolProfile = item, SymbolProfileId = item.Id, StockPrices = prices, LastUpdate = dateTime };
//					logger.LogDebug("Got stock prices for {symbol}", item.Symbol);
//					await dbContext.SaveChangesAsync();
//				}
//				catch
//				{
//					logger.LogWarning("Failed to get stock prices for {symbol}", item.Symbol);
//				}
//			}
//		}
//	}
//}
