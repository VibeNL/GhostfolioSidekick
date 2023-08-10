//using GhostfolioSidekick.Ghostfolio.API;
//using System.Collections.Generic;

//namespace GhostfolioSidekick.FileImporter.ScalableCaptial
//{

//	public class ScalableCapitalParser : IFileImporter
//	{
//		private IEnumerable<IFileImporter> fileImporters;

//		private IGhostfolioAPI api;

//		public ScalableCapitalParser(IGhostfolioAPI api)
//		{
//			this.api = api;
//			fileImporters = new IFileImporter[] {
//				new BaaderBankRKK(api),
//				new BaaderBankWUM(api),
//			};
//		}

//		public async Task<bool> CanConvertOrders(IEnumerable<string> filenames)
//		{
//			return filenames.All(y => fileImporters.Any(x => x.CanConvertOrders(new[] { y }).Result));
//		}

//		public Task<IEnumerable<Order>> ConvertToOrders(string accountName, IEnumerable<string> filenames)
//		{
//			var resultList = new List<Order>();
//			var orders = filenames
//			.SelectMany(y => 
//				fileImporters.Single(x => 
//					x.CanConvertOrders(new[] { y }).Result)
//					.ConvertToOrders(accountName, new[] { y }).Result)
//			.ToList();

//			// Match Fee with Transaction
//			var group = orders.GroupBy(x => x.Comment);
//			foreach (var item in group)
//			{
//				if (item.Count() == 1)
//				{
//					resultList.Add(item.Single());
//				}
//				else 
//				{
//					var transaction = item.Where(x => x.Quantity > 0).DistinctBy(x => new { x.AccountId, x.Asset, x.Currency, x.Date, x.UnitPrice, x.Quantity }).Single();
//					var fee = item.Where(x => x.Quantity == -1).DistinctBy(x => new { x.AccountId, x.Asset, x.Currency, x.Date, x.UnitPrice, x.Quantity }).SingleOrDefault();

//					if (fee != null)
//					{
//						transaction.Fee = fee.UnitPrice;
//					}

//					resultList.Add(transaction);
//				}
//			}

//			return Task.FromResult(resultList.Where(x => x.Type != OrderType.FEE));
//		}
//	}
//}
