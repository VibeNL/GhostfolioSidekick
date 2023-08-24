using CsvHelper.Configuration;
using GhostfolioSidekick.Crypto;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Coinbase
{
	public class CoinbaseParser : RecordBaseImporter<CoinbaseRecord>
	{
		private IGhostfolioAPI api;
		private NumberStyles numberStyle = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint;

		public CoinbaseParser(IGhostfolioAPI api) : base(api)
		{
			this.api = api;
		}

		protected override async Task<IEnumerable<Order>> ConvertOrders(CoinbaseRecord record, Account account, IEnumerable<CoinbaseRecord> allRecords)
		{
			var orderTypeCrypto = GetOrderTypeCrypto(record);
			var orderType = Convert(orderTypeCrypto);
			if (orderType == null)
			{
				return Array.Empty<Order>();
			}

			var assetName = record.Asset;
			var asset = await api.FindSymbolByISIN(CryptoTranslate.Instance.TranslateToken(assetName), x =>
				ParseResult(CryptoTranslate.Instance.TranslateToken(assetName), assetName, x));

			var refCode = $"{orderType}_{assetName}_{record.Timestamp.Ticks}";

			var orders = new List<Order>();

			var order = new Order
			{
				AccountId = account.Id,
				Asset = asset,
				Currency = record.Currency,
				Date = record.Timestamp.ToUniversalTime(),
				Comment = $"Transaction Reference: [{refCode}]",
				Fee = record.Fee ?? 0,
				FeeCurrency = record.Currency,
				Quantity = record.Quantity,
				Type = orderType.Value,
				UnitPrice = await GetCorrectUnitPrice(record.UnitPrice, asset, record.Timestamp.ToUniversalTime()),
				ReferenceCode = refCode,
			};
			orders.Add(order);

			// Convert is a SELL / BUY. SELL in the default operation so we need another BUY
			if (orderTypeCrypto.GetValueOrDefault() == CryptoOrderType.Convert)
			{
				var buyRecord = ParseComment4Convert(record);
				var assetBuy = await api.FindSymbolByISIN(CryptoTranslate.Instance.TranslateToken(buyRecord.Asset), x =>
					ParseResult(CryptoTranslate.Instance.TranslateToken(buyRecord.Asset), buyRecord.Asset, x));

				var refCodeBuy = $"{OrderType.BUY}_{buyRecord.Asset}_{record.Timestamp.Ticks}";
				var orderBuy = new Order
				{
					AccountId = account.Id,
					Asset = assetBuy,
					Currency = record.Currency,
					Date = record.Timestamp.ToUniversalTime(),
					Comment = $"Transaction Reference: [{refCodeBuy}]",
					Fee = 0,
					FeeCurrency = record.Currency,
					Quantity = buyRecord.Quantity,
					Type = OrderType.BUY,
					UnitPrice = await GetCorrectUnitPrice(buyRecord.UnitPrice, assetBuy, record.Timestamp.ToUniversalTime()),
					ReferenceCode = refCodeBuy,
				};
				orders.Add(orderBuy);
			}

			return orders;
		}

		private async Task<decimal> GetCorrectUnitPrice(decimal? originalUnitPrice, Asset? symbol, DateTime date)
		{
			if (originalUnitPrice > 0)
			{
				return originalUnitPrice.Value;
			}

			// GetPrice from Ghostfolio
			var price = await api.GetMarketPrice(symbol, date);
			return price;
		}

		private Asset? ParseResult(string assetName, string symbol, IEnumerable<Asset> assets)
		{
			var cryptoOnly = assets.Where(x => x.AssetSubClass == "CRYPTOCURRENCY");
			var asset = cryptoOnly.FirstOrDefault(x => assetName == x.Name);
			if (asset != null)
			{
				return asset;
			}

			asset = cryptoOnly.FirstOrDefault(x => symbol == x.Symbol);
			if (asset != null)
			{
				return asset;
			}

			asset = cryptoOnly
				.OrderBy(x => x.Symbol.Length)
				.FirstOrDefault();

			return asset;
		}

		private (decimal Quantity, decimal UnitPrice, string Asset) ParseComment4Convert(CoinbaseRecord record)
		{
			// Converted 0.00052203 ETH to 0.087842 ATOM
			var comment = record.Notes.Split(" ");
			var quantity = decimal.Parse(comment[4], numberStyle, CultureInfo.InvariantCulture);

			var unitPrice = (record.UnitPrice * record.Quantity) / quantity;
			return (quantity, unitPrice.Value, comment[5]);
		}

		private OrderType? Convert(CryptoOrderType? value)
		{
			if (value is null)
			{
				return null;
			}

			return value switch
			{
				CryptoOrderType.Buy => (OrderType?)OrderType.BUY,
				CryptoOrderType.Sell => (OrderType?)OrderType.SELL,
				CryptoOrderType.Send => (OrderType?)OrderType.SELL,
				CryptoOrderType.Receive => (OrderType?)OrderType.BUY,
				CryptoOrderType.Convert => (OrderType?)OrderType.SELL,
				CryptoOrderType.LearningReward or CryptoOrderType.StakingReward => null,
				_ => throw new NotSupportedException(),
			};
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}

		protected override StreamReader GetStreamReader(string file)
		{
			var sr = base.GetStreamReader(file);

			for (var i = 0; i < 7; i++)
			{
				sr.ReadLine();
			}

			return sr;
		}

		private CryptoOrderType? GetOrderTypeCrypto(CoinbaseRecord record)
		{
			switch (record.Order)
			{
				case "Buy":
					return CryptoOrderType.Buy;
				case "Receive":
					return CryptoOrderType.Receive;
				case "Send":
					return CryptoOrderType.Send;
				case "Sell":
					return CryptoOrderType.Sell;
				case "Convert":
					return CryptoOrderType.Convert;
				case "Rewards Income":
					return CryptoOrderType.StakingReward;
				case "Learning Reward":
					return CryptoOrderType.LearningReward;
				default: throw new NotSupportedException($"{record.Order}");
			}
		}
	}
}
