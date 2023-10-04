using CsvHelper.Configuration;
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

		protected override async Task<IEnumerable<Model.Activity>> ConvertOrders(NexoRecord record, Model.Account account, IEnumerable<NexoRecord> allRecords)
		{
			var activityType = GetOrderTypeCrypto(record);
			if (activityType == null || !record.Details.StartsWith("approved"))
			{
				return Array.Empty<Model.Activity>();
			}

			var currency = CurrencyHelper.ParseCurrency("USD");
			var orders = new List<Model.Activity>();
			var assetName = record.InputCurrency;

			if (!fiat.Contains(assetName))
			{
				var asset = await api.FindSymbolByISIN(assetName, x =>
					ParseFindSymbolByISINResult(assetName, assetName, x));

				var order = new Model.Activity
				{
					Asset = asset,
					Date = record.DateTime,
					Comment = $"Transaction Reference: [{record.Transaction}]",
					Fee = null,
					Quantity = record.InputAmount,
					ActivityType = HandleConvertActivityType(activityType.Value),
					UnitPrice = new Model.Money(currency, record.GetUSDEquivalent() / record.InputAmount, record.DateTime),
					ReferenceCode = record.Transaction,
				};
				orders.Add(order);
			}

			// Convert is a SELL / BUY. SELL in the default operation so we need another BUY
			if (activityType.GetValueOrDefault() == Model.ActivityType.Convert)
			{
				var buyAsset = record.OutputCurrency;
				if (!fiat.Contains(buyAsset))
				{
					var assetBuy = await api.FindSymbolByISIN(buyAsset, x =>
						ParseFindSymbolByISINResult(buyAsset, buyAsset, x));

					var orderBuy = new Model.Activity
					{
						Asset = assetBuy,
						Date = record.DateTime,
						Comment = $"Transaction Reference: [{record.Transaction}]",
						Fee = null,
						Quantity = record.OutputAmount,
						ActivityType = Model.ActivityType.Buy,
						UnitPrice = new Model.Money(currency, record.GetUSDEquivalent() / record.OutputAmount, record.DateTime),
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

		private Model.ActivityType? GetOrderTypeCrypto(NexoRecord record)
		{
			switch (record.Type)
			{
				case "ReferralBonus": // TODO: Should be a 'reward'
				case "Deposit":
					return Model.ActivityType.Buy;
				case "Exchange":
					return Model.ActivityType.Convert;
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
