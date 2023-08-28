using CsvHelper.Configuration;
using GhostfolioSidekick.Crypto;
using GhostfolioSidekick.Ghostfolio.API;
using System.Data;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class NexoParser : CryptoRecordBaseImporter<NexoRecord>
	{
		private string[] fiat = new[] { "EURX", "USDX" };

		public NexoParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Order>> ConvertOrders(NexoRecord record, Account account, IEnumerable<NexoRecord> allRecords)
		{
			var orderTypeCrypto = GetOrderTypeCrypto(record);
			var orderType = Convert(orderTypeCrypto);
			if (orderType == null)
			{
				return Array.Empty<Order>();
			}

			var orders = new List<Order>();
			var assetName = record.InputCurrency;

			if (!fiat.Contains(assetName))
			{
				var asset = await api.FindSymbolByISIN(CryptoTranslate.Instance.TranslateToken(assetName), x =>
					ParseFindSymbolByISINResult(CryptoTranslate.Instance.TranslateToken(assetName), assetName, x));

				var order = new Order
				{
					AccountId = account.Id,
					Asset = asset,
					Currency = "USD",
					Date = record.DateTime,
					Comment = $"Transaction Reference: [{record.Transaction}]",
					Fee = 0,
					FeeCurrency = null,
					Quantity = record.InputAmount,
					Type = orderType.Value,
					UnitPrice = record.GetUSDEquivalent() / record.InputAmount,
					ReferenceCode = record.Transaction,
				};
				orders.Add(order);
			}

			// Convert is a SELL / BUY. SELL in the default operation so we need another BUY
			if (orderTypeCrypto.GetValueOrDefault() == CryptoOrderType.Convert)
			{
				var buyAsset = record.OutputCurrency;
				if (!fiat.Contains(buyAsset))
				{
					var assetBuy = await api.FindSymbolByISIN(CryptoTranslate.Instance.TranslateToken(buyAsset), x =>
						ParseFindSymbolByISINResult(CryptoTranslate.Instance.TranslateToken(buyAsset), buyAsset, x));

					var orderBuy = new Order
					{
						AccountId = account.Id,
						Asset = assetBuy,
						Currency = "USD",
						Date = record.DateTime,
						Comment = $"Transaction Reference: [{record.Transaction}]",
						Fee = 0,
						FeeCurrency = null,
						Quantity = record.OutputAmount,
						Type = OrderType.BUY,
						UnitPrice = record.GetUSDEquivalent() / record.OutputAmount,
						ReferenceCode = record.Transaction,
					};
					orders.Add(orderBuy);
				}
			}

			// Filter out fiat currency
			return orders;
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

		private CryptoOrderType? GetOrderTypeCrypto(NexoRecord record)
		{
			switch (record.Type)
			{
				case "Deposit":
					return CryptoOrderType.Buy;
				case "Exchange":
					return CryptoOrderType.Convert;
				case "LockingTermDeposit":
				case "DepositToExchange":
				case "ExchangeDepositedOn":
				case "ReferralBonus":
					return null;
				default: throw new NotSupportedException($"{record.Type}");
			}
		}
	}
}
