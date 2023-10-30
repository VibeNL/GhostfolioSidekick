using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class NexoParser : CryptoRecordBaseImporter<NexoRecord>
	{
		private Asset[] fiatCoin = new[] {
			new Asset(new Currency("EUR"), "EURX",null, "EURX", null, null, null),
			new Asset(new Currency("USD"), "USDX",null, "USDX", null, null, null),
			new Asset(new Currency("EUR"), "EUR", null,"EUR", null, null, null),
			new Asset(new Currency("USD"), "USD",null, "USD", null, null, null)
		};

		public NexoParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(NexoRecord record, Account account, IEnumerable<NexoRecord> allRecords)
		{
			if (!record.Details.StartsWith("approved"))
			{
				return Array.Empty<Activity>();
			}

			var activities = new List<Activity>();

			var inputAsset = await GetAsset(record.InputCurrency);
			var inputActivity = new Activity
			{
				Asset = inputAsset,
				Date = record.DateTime,
				Comment = TransactionReferenceUtilities.GetComment(record.Transaction, record.InputCurrency),
				Fee = null,
				Quantity = Math.Abs(record.InputAmount),
				ActivityType = ActivityType.Undefined,
				UnitPrice = GetUnitPrice(record, false),
				ReferenceCode = record.Transaction,
			};

			var outputAsset = await GetAsset(record.OutputCurrency);
			var refCode = record.Transaction;
			var outputActivity = new Activity
			{
				Asset = outputAsset,
				Date = record.DateTime,
				Comment = TransactionReferenceUtilities.GetComment(refCode, record.OutputCurrency),
				Fee = null,
				Quantity = Math.Abs(record.OutputAmount),
				ActivityType = ActivityType.Undefined,
				UnitPrice = GetUnitPrice(record, true),
				ReferenceCode = refCode,
			};

			activities.AddRange(HandleRecord(record, inputActivity, outputActivity));

			return activities;

			async Task<Asset?> GetAsset(string assetName)
			{
				if (fiatCoin.Any(x => x.Symbol == assetName))
				{
					return null;
				}

				return await api.FindSymbolByIdentifier(assetName, x =>
								ParseFindSymbolByISINResult(assetName, assetName, x));
			}
		}

		private IEnumerable<Activity> HandleRecord(NexoRecord record, Activity inputActivity, Activity outputActivity)
		{
			switch (record.Type)
			{
				case "Exchange Cashback":
				case "ReferralBonus": // TODO: Should be a 'reward'
				case "Deposit":
					return new[] { SetActivity(outputActivity, ActivityType.Receive) };
				case "ExchangeDepositedOn":
				case "Exchange":
					return HandleConversion(inputActivity, outputActivity, record);
				case "Interest":
				case "FixedTermInterest":
				// return new[] { SetActivity(outputActivity, ActivityType.Interest) }; // Staking rewards are not yet supported
				case "DepositToExchange":
				case "LockingTermDeposit":
				case "UnlockingTermDeposit":
					return Enumerable.Empty<Activity>();
				default: throw new NotSupportedException($"{record.Type}");
			}
		}

		private Activity SetActivity(Activity outputActivity, ActivityType activityType)
		{
			outputActivity.ActivityType = activityType;
			return outputActivity;
		}

		private IEnumerable<Activity> HandleConversion(Activity inputActivity, Activity outputActivity, NexoRecord record)
		{
			var inFiatCoin = inputActivity.Asset == null && fiatCoin.Any(x => x.Symbol == inputActivity.UnitPrice.Currency.Symbol);
			var outFiatCoin = outputActivity.Asset == null && fiatCoin.Any(x => x.Symbol == outputActivity.UnitPrice.Currency.Symbol);

			if (inFiatCoin && !outFiatCoin)
			{
				outputActivity.ActivityType = ActivityType.Buy;
				return new[] { outputActivity };
			}
			else if (!inFiatCoin && outFiatCoin)
			{
				inputActivity.ActivityType = ActivityType.Sell;
				return new[] { inputActivity };
			}
			else if (inFiatCoin)
			{
				inputActivity.ActivityType = ActivityType.CashDeposit; // TODO: withdrawal check?
				return new[] { inputActivity };
			}
			else
			{
				outputActivity.ReferenceCode += "_2";
				outputActivity.Comment = TransactionReferenceUtilities.GetComment(outputActivity.ReferenceCode, record.OutputCurrency);
				inputActivity.ActivityType = ActivityType.Sell;
				outputActivity.ActivityType = ActivityType.Buy;
				return new[] { inputActivity, outputActivity };
			}
		}

		private Money GetUnitPrice(NexoRecord record, bool isOutput)
		{
			var currency = isOutput ? record.OutputCurrency : record.InputCurrency;
			var amount = isOutput ? record.OutputAmount : record.InputAmount;
			var fiatCoinCurrency = fiatCoin.Any(x => x.Symbol == currency);

			if (!fiatCoinCurrency)
			{
				return new Money("USD", Math.Abs(record.GetUSDEquivalent() / amount), record.DateTime);
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
	}
}
