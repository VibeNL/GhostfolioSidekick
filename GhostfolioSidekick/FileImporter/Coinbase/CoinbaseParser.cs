using CsvHelper.Configuration;
using GhostfolioSidekick.Crypto;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Coinbase
{
	public class CoinbaseParser : RecordBaseImporter<CoinbaseRecord>
	{
		private IGhostfolioAPI api;

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

			var asset = await api.FindSymbolByISIN(CryptoTranslate.Instance.TranslateToken(record.Asset));

			var refCode = $"{orderType}_{record.Asset}_{record.Timestamp.Ticks}";

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
				UnitPrice = record.UnitPrice ?? 0,
				ReferenceCode = refCode,
			};
			orders.Add(order);

			if (orderTypeCrypto.GetValueOrDefault() == CryptoOrderType.Convert)
			{
				ParseComment4Convert(record.Notes);
				var assetBuy = await api.FindSymbolByISIN(CryptoTranslate.Instance.TranslateToken(record.Asset));

				var refCodeBuy = $"{OrderType.BUY}_{record.Asset}_{record.Timestamp.Ticks}";
				var orderBuy = new Order
				{
					AccountId = account.Id,
					Asset = asset,
					Currency = record.Currency,
					Date = record.Timestamp.ToUniversalTime(),
					Comment = $"Transaction Reference: [{refCodeBuy}]",
					Fee = 0,
					FeeCurrency = record.Currency,
					Quantity = record.Quantity,
					Type = OrderType.BUY,
					UnitPrice = -1,
					ReferenceCode = refCodeBuy,
				};
				orders.Add(orderBuy);
			}

			return orders;
		}

		private void ParseComment4Convert(string notes)
		{
			throw new NotImplementedException();
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
