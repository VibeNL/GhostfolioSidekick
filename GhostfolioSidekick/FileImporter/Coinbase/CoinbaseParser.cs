//using CsvHelper.Configuration;
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

//		protected override async Task<IEnumerable<Model.Activity>> ConvertOrders(CoinbaseRecord record, Model.Account account, IEnumerable<CoinbaseRecord> allRecords)
//		{
//			var activityType = GetOrderTypeCrypto(record);
//			if (activityType == null)
//			{
//				return Array.Empty<Model.Activity>();
//			}

//			var assetName = record.Asset;
//			var asset = await api.FindSymbolByISIN(assetName, x =>
//				ParseFindSymbolByISINResult(assetName, assetName, x));

//			var refCode = $"{HandleConvertActivityType(activityType.Value)}_{assetName}_{record.Timestamp.ToUniversalTime().Ticks}";

//			var orders = new List<Model.Activity>();

//			var order = new Model.Activity
//			{
//				Asset = asset,
//				Date = record.Timestamp.ToUniversalTime(),
//				Comment = $"Transaction Reference: [{refCode}]",
//				Fee = record.Fee == null ? null : new Model.Money(record.Currency, record.Fee ?? 0, record.Timestamp.ToUniversalTime()),
//				Quantity = record.Quantity,
//				ActivityType = HandleConvertActivityType(activityType.Value),
//				UnitPrice = await GetCorrectUnitPrice(new Model.Money(record.Currency, record.UnitPrice ?? 0, record.Timestamp.ToUniversalTime()), asset, record.Timestamp.ToUniversalTime()),
//				ReferenceCode = refCode,
//			};
//			orders.Add(order);

//			// Convert is a SELL / BUY. SELL in the default operation so we need another BUY
//			if (activityType.GetValueOrDefault() == Model.ActivityType.Convert)
//			{
//				var buyRecord = ParseComment4Convert(record);
//				var assetBuy = await api.FindSymbolByISIN(buyRecord.Asset, x =>
//					ParseFindSymbolByISINResult(buyRecord.Asset, buyRecord.Asset, x));

//				var refCodeBuy = $"{Model.ActivityType.Buy}_{buyRecord.Asset}_{record.Timestamp.ToUniversalTime().Ticks}";
//				var orderBuy = new Model.Activity
//				{
//					Asset = assetBuy,
//					Date = record.Timestamp.ToUniversalTime(),
//					Comment = $"Transaction Reference: [{refCodeBuy}]",
//					Fee = null,
//					Quantity = buyRecord.Quantity,
//					ActivityType = Model.ActivityType.Buy,
//					UnitPrice = await GetCorrectUnitPrice(new Model.Money(record.Currency, buyRecord.UnitPrice, record.Timestamp.ToUniversalTime()), assetBuy, record.Timestamp.ToUniversalTime()),
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

//		private Model.ActivityType? GetOrderTypeCrypto(CoinbaseRecord record)
//		{
//			switch (record.Order)
//			{
//				case "Buy":
//					return Model.ActivityType.Buy;
//				case "Receive":
//					return Model.ActivityType.Receive;
//				case "Send":
//					return Model.ActivityType.Send;
//				case "Sell":
//					return Model.ActivityType.Sell;
//				case "Convert":
//					return Model.ActivityType.Convert;
//				case "Rewards Income":
//					return Model.ActivityType.StakingReward;
//				case "Learning Reward":
//					return Model.ActivityType.LearningReward;
//				default: throw new NotSupportedException($"{record.Order}");
//			}
//		}
//	}
//}
