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

		protected override async Task<Order?> ConvertOrder(CoinbaseRecord record, Account account, IEnumerable<CoinbaseRecord> allRecords)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return null;
			}

			var asset = await api.FindSymbolByISIN(CryptoTranslate.Instance.TranslateToken(record.Asset));

			var refCode = $"{orderType}_{record.Asset}_{record.Timestamp.Ticks}";

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

			return order;
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

		private OrderType? GetOrderType(CoinbaseRecord record)
		{
			switch (record.Order)
			{
				case "Buy":
				case "Receive":
					return OrderType.BUY;
				case "Send":
				case "Sell":
					return OrderType.SELL;
				case "Convert":
				case "Rewards Income":
					return null;
				default: throw new NotSupportedException($"{record.Order}");
			}
		}
	}
}
