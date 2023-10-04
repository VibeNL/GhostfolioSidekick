//using CsvHelper.Configuration;
//using GhostfolioSidekick.Crypto;
//using GhostfolioSidekick.Ghostfolio.API;
//using System.Globalization;

//namespace GhostfolioSidekick.FileImporter.Coinbase
//{
//	public class CoinbaseParser : CryptoRecordBaseImporter<CoinbaseRecord>
//	{
//		private readonly NumberStyles numberStyle = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint;

//		public CoinbaseParser(IGhostfolioAPI api) : base(api)
//		{
//		}

//		protected override async Task<IEnumerable<Activity>> ConvertOrders(CoinbaseRecord record, Account account, IEnumerable<CoinbaseRecord> allRecords)
//		{
//			var orderTypeCrypto = GetOrderTypeCrypto(record);
//			var orderType = Convert(orderTypeCrypto);
//			if (orderType == null)
//			{
//				return Array.Empty<Activity>();
//			}

//			var assetName = record.Asset;
//			var asset = await api.FindSymbolByISIN(assetName, x =>
//				ParseFindSymbolByISINResult(assetName, assetName, x));

//			var refCode = $"{orderType}_{assetName}_{record.Timestamp.ToUniversalTime().Ticks}";

//			var orders = new List<Activity>();

//			var order = new Activity
//			{
//				AccountId = account.Id,
//				Asset = asset,
//				Currency = record.Currency,
//				Date = record.Timestamp.ToUniversalTime(),
//				Comment = $"Transaction Reference: [{refCode}]",
//				Fee = record.Fee ?? 0,
//				FeeCurrency = record.Currency,
//				Quantity = record.Quantity,
//				Type = orderType.Value,
//				UnitPrice = await GetCorrectUnitPrice(record.UnitPrice, asset, record.Timestamp.ToUniversalTime()),
//				ReferenceCode = refCode,
//			};
//			orders.Add(order);

//			// Convert is a SELL / BUY. SELL in the default operation so we need another BUY
//			if (orderTypeCrypto.GetValueOrDefault() == CryptoOrderType.Convert)
//			{
//				var buyRecord = ParseComment4Convert(record);
//				var assetBuy = await api.FindSymbolByISIN(buyRecord.Asset, x =>
//					ParseFindSymbolByISINResult(buyRecord.Asset, buyRecord.Asset, x));

//				var refCodeBuy = $"{ActivityType.BUY}_{buyRecord.Asset}_{record.Timestamp.ToUniversalTime().Ticks}";
//				var orderBuy = new Activity
//				{
//					AccountId = account.Id,
//					Asset = assetBuy,
//					Currency = record.Currency,
//					Date = record.Timestamp.ToUniversalTime(),
//					Comment = $"Transaction Reference: [{refCodeBuy}]",
//					Fee = 0,
//					FeeCurrency = record.Currency,
//					Quantity = buyRecord.Quantity,
//					Type = ActivityType.BUY,
//					UnitPrice = await GetCorrectUnitPrice(buyRecord.UnitPrice, assetBuy, record.Timestamp.ToUniversalTime()),
//					ReferenceCode = refCodeBuy,
//				};
//				orders.Add(orderBuy);
//			}

//			return orders;
//		}

//		private (decimal Quantity, decimal UnitPrice, string Asset) ParseComment4Convert(CoinbaseRecord record)
//		{
//			// Converted 0.00052203 ETH to 0.087842 ATOM
//			var comment = record.Notes.Split(" ");
//			var quantity = decimal.Parse(comment[4], numberStyle, CultureInfo.InvariantCulture);

//			var unitPrice = (record.UnitPrice * record.Quantity) / quantity;
//			return (quantity, unitPrice.Value, comment[5]);
//		}

//		protected override CsvConfiguration GetConfig()
//		{
//			return new CsvConfiguration(CultureInfo.InvariantCulture)
//			{
//				HasHeaderRecord = true,
//				CacheFields = true,
//				Delimiter = ",",
//			};
//		}

//		protected override StreamReader GetStreamReader(string file)
//		{
//			var sr = base.GetStreamReader(file);

//			for (var i = 0; i < 7; i++)
//			{
//				sr.ReadLine();
//			}

//			return sr;
//		}

//		private CryptoOrderType? GetOrderTypeCrypto(CoinbaseRecord record)
//		{
//			switch (record.Order)
//			{
//				case "Buy":
//					return CryptoOrderType.Buy;
//				case "Receive":
//					return CryptoOrderType.Receive;
//				case "Send":
//					return CryptoOrderType.Send;
//				case "Sell":
//					return CryptoOrderType.Sell;
//				case "Convert":
//					return CryptoOrderType.Convert;
//				case "Rewards Income":
//					return CryptoOrderType.StakingReward;
//				case "Learning Reward":
//					return CryptoOrderType.LearningReward;
//				default: throw new NotSupportedException($"{record.Order}");
//			}
//		}
//	}
//}
