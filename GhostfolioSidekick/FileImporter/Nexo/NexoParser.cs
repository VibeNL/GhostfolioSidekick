using CsvHelper.Configuration;
using GhostfolioSidekick.Crypto;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class NexoParser : CryptoRecordBaseImporter<NexoRecord>
	{
		private string[] fiat = new[] { "EURX", "USDX" };

		public NexoParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(NexoRecord record, Account account, IEnumerable<NexoRecord> allRecords)
		{
			var orderTypeCrypto = GetOrderTypeCrypto(record);
			var orderType = Convert(orderTypeCrypto);
			if (orderType == null || !record.Details.StartsWith("approved"))
			{
				return Array.Empty<Activity>();
			}

			var orders = new List<Activity>();
			var assetName = record.InputCurrency;

			if (!fiat.Contains(assetName))
			{
				var asset = await api.FindSymbolByISIN(assetName, x =>
					ParseFindSymbolByISINResult(assetName, assetName, x));

				var order = new Activity
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
					var assetBuy = await api.FindSymbolByISIN(buyAsset, x =>
						ParseFindSymbolByISINResult(buyAsset, buyAsset, x));

					var orderBuy = new Activity
					{
						AccountId = account.Id,
						Asset = assetBuy,
						Currency = "USD",
						Date = record.DateTime,
						Comment = $"Transaction Reference: [{record.Transaction}]",
						Fee = 0,
						FeeCurrency = null,
						Quantity = record.OutputAmount,
						Type = ActivityType.BUY,
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
				case "ReferralBonus": // TODO: Should be a 'reward'
				case "Deposit":
					return CryptoOrderType.Buy;
				case "Exchange":
					return CryptoOrderType.Convert;
				case "LockingTermDeposit":
				case "UnlockingTermDeposit":
				case "DepositToExchange":
				case "ExchangeDepositedOn":
				case "FixedTermInterest": // TODO: Should be a 'reward'
				case "Interest": // TODO: Should be a 'reward'
					return null;
				default: throw new NotSupportedException($"{record.Type}");
			}
		}
	}
}
