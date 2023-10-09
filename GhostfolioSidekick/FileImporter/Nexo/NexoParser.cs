using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class NexoParser : CryptoRecordBaseImporter<NexoRecord>
	{
		private Model.Asset[] fiat = new[] {
			new Model.Asset(new Currency("EUR"), "EUR", "EUR", null, null, null),
			new Model.Asset(new Currency("USD"), "USD", "USD", null, null, null)
		};

		private Model.Asset[] fiatCoin = new[] {
			new Model.Asset(new Currency("EUR"), "EURX", "EURX", null, null, null),
			new Model.Asset(new Currency("USD"), "USDX", "USDX", null, null, null)
		};

		public NexoParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Model.Activity>> ConvertOrders(NexoRecord record, Model.Account account, IEnumerable<NexoRecord> allRecords)
		{
			var activityType = GetActivityTypeCrypto(record);
			if (activityType == null || !record.Details.StartsWith("approved"))
			{
				return Array.Empty<Model.Activity>();
			}

			var activities = new List<Model.Activity>();

			var inputAsset = await GetAsset(record.InputCurrency);
			var inputActivity = new Model.Activity
			{
				Asset = inputAsset,
				Date = record.DateTime,
				Comment = $"Transaction Reference: [{record.Transaction}]",
				Fee = null,
				Quantity = record.InputAmount,
				ActivityType = activityType.Value,
				UnitPrice = GetUnitPrice(record, false),
				ReferenceCode = record.Transaction,
			};

			var outputAsset = await GetAsset(record.OutputCurrency);
			var refCode = record.Transaction;
			var outputActivity = new Model.Activity
			{
				Asset = outputAsset,
				Date = record.DateTime,
				Comment = $"Transaction Reference: [{refCode}]",
				Fee = null,
				Quantity = record.OutputAmount,
				ActivityType = activityType.Value,
				UnitPrice = GetUnitPrice(record, true),
				ReferenceCode = refCode,
			};

			switch (activityType)
			{
				case Model.ActivityType.Buy:
				case Model.ActivityType.Receive:
				case Model.ActivityType.Dividend:
				case Model.ActivityType.Interest:
				case Model.ActivityType.Gift:
				case Model.ActivityType.CashDeposit:
				case Model.ActivityType.CashWithdrawal:
					activities.Add(outputActivity);
					break;
				case Model.ActivityType.Sell:
				case Model.ActivityType.Send:
					activities.Add(inputActivity);
					break;
				case Model.ActivityType.Convert:
					activities.AddRange(HandleConversion(inputActivity, outputActivity));
					break;
				case Model.ActivityType.LearningReward:
				case Model.ActivityType.StakingReward:
				default:
					throw new NotSupportedException();
			}

			return activities;

			async Task<Model.Asset?> GetAsset(string assetName)
			{
				return await api.FindSymbolByISIN(assetName, x =>
								ParseFindSymbolByISINResult(assetName, assetName, x));
			}
		}

		private IEnumerable<Model.Activity> HandleConversion(Model.Activity inputActivity, Model.Activity outputActivity)
		{
			//var inFiat = fiat.Any(x => x.Symbol == inputActivity.UnitPrice.Currency.Symbol);
			//var outFiat = fiat.Any(x => x.Symbol == outputActivity.UnitPrice.Currency.Symbol);
			var inFiatCoin = fiatCoin.Any(x => x.Symbol == inputActivity.UnitPrice.Currency.Symbol);
			var outFiatCoin = fiatCoin.Any(x => x.Symbol == outputActivity.UnitPrice.Currency.Symbol);

			if (inFiatCoin && !outFiatCoin)
			{
				outputActivity.ActivityType = Model.ActivityType.Buy;
				return new[] { outputActivity };
			}
			if (!inFiatCoin && outFiatCoin)
			{
				inputActivity.ActivityType = Model.ActivityType.Sell;
				return new[] { inputActivity };
			}

			throw new NotSupportedException();
		}

		private Money GetUnitPrice(NexoRecord record, bool isOutput)
		{
			var currency = isOutput ? record.OutputCurrency : record.InputCurrency;
			var amount = isOutput ? record.OutputAmount : record.InputAmount;
			var fiatCoinCurrency = fiatCoin.Any(x => x.Symbol == currency);

			if (fiatCoinCurrency == false)
			{
				return new Money("USD", record.GetUSDEquivalent() / amount, record.DateTime);
			}

			return new Money(currency, 1, record.DateTime);
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

		private Model.ActivityType? GetActivityTypeCrypto(NexoRecord record)
		{
			switch (record.Type)
			{
				case "ReferralBonus": // TODO: Should be a 'reward'
				case "Deposit":
					return Model.ActivityType.Receive;
				case "Exchange":
					return Model.ActivityType.Convert;
				case "DepositToExchange":
					return Model.ActivityType.CashDeposit;
				case "Interest":
					return Model.ActivityType.Interest;
				case "LockingTermDeposit":
				case "UnlockingTermDeposit":
				case "ExchangeDepositedOn":
				case "FixedTermInterest": // TODO: Should be a 'reward'
					return null;
				default: throw new NotSupportedException($"{record.Type}");
			}
		}
	}
}
