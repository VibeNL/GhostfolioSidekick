using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class NexoParser : CryptoRecordBaseImporter<NexoRecord>
	{
		private Model.Asset[] fiat = new[] {
			new Model.Asset(new Model.Currency("EUR"), "EUR", "EUR", null, null, null),
			new Model.Asset(new Model.Currency("USD"), "USD", "USD", null, null, null)
		};

		private Model.Asset[] fiatCoin = new[] {
			new Model.Asset(new Model.Currency("EUR"), "EURX", "EURX", null, null, null),
			new Model.Asset(new Model.Currency("USD"), "USDX", "USDX", null, null, null)
		};

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

			var activities = new List<Model.Activity>();
			if (HandleDepositAndWithdrawel(record, activities))
			{
				return activities;
			}

			var assetName = record.InputCurrency;
			var sellAsset = await GetAsset(assetName);

			var sellActivity = new Model.Activity
			{
				Asset = sellAsset,
				Date = record.DateTime,
				Comment = $"Transaction Reference: [{record.Transaction}]",
				Fee = null,
				Quantity = record.InputAmount,
				ActivityType = HandleConvertActivityType(activityType.Value),
				UnitPrice = GetUnitPrice(record, false),
				ReferenceCode = record.Transaction,
			};
			activities.Add(sellActivity);

			var buyAssetName = record.OutputCurrency;
			var buyAsset = await GetAsset(buyAssetName);

			var refCode = record.Transaction + "_2";
			var orderBuy = new Model.Activity
			{
				Asset = buyAsset,
				Date = record.DateTime,
				Comment = $"Transaction Reference: [{refCode}]",
				Fee = null,
				Quantity = record.OutputAmount,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = GetUnitPrice(record, true),
				ReferenceCode = refCode,
			};

			if (activityType != Model.ActivityType.Receive)
			{
				activities.Add(orderBuy);
			}

			// Filter out fiat currency
			return activities.Where(FilterEmptyBuysAndSells);

			async Task<Model.Asset?> GetAsset(string assetName)
			{
				return await api.FindSymbolByISIN(assetName, x =>
								ParseFindSymbolByISINResult(assetName, assetName, x));
			}
		}

		private bool FilterEmptyBuysAndSells(Model.Activity activity)
		{
			switch (activity.ActivityType)
			{
				case Model.ActivityType.Buy:
				case Model.ActivityType.Sell:
					return activity.Asset != null;
				case Model.ActivityType.Dividend:
				case Model.ActivityType.Send:
				case Model.ActivityType.Receive:
				case Model.ActivityType.Interest:
				case Model.ActivityType.Gift:
				case Model.ActivityType.LearningReward:
				case Model.ActivityType.StakingReward:
				case Model.ActivityType.Convert:
				case Model.ActivityType.CashDeposit:
				case Model.ActivityType.CashWithdrawal:
					return true;
				default:
					throw new NotSupportedException();
			}
		}

		protected override void SetActivitiesToAccount(Model.Account account, ICollection<Model.Activity> values)
		{
			base.SetActivitiesToAccount(account, values);

			// Fix balance
			account.Balance.Empty();
			account.Balance.Calculate(values.Where(x => x.Asset == null).ToList());
		}

		private Money GetUnitPrice(NexoRecord record, bool isOutput)
		{
			var currency = isOutput ? record.OutputCurrency : record.InputCurrency;
			var amount = isOutput ? record.OutputAmount : record.InputAmount;
			var fiatCoinCurrency = fiatCoin.Any(x => x.Symbol == currency);

			if (fiatCoinCurrency == false)
			{
				return new Model.Money("USD", record.GetUSDEquivalent() / amount, record.DateTime);
			}

			return new Model.Money(currency, 1, record.DateTime);
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
					return Model.ActivityType.Receive;
				case "Exchange":
					return Model.ActivityType.Convert;
				case "DepositToExchange":
					return Model.ActivityType.CashDeposit;
				case "LockingTermDeposit":
				case "UnlockingTermDeposit":
				case "ExchangeDepositedOn":
				case "FixedTermInterest": // TODO: Should be a 'reward'
				case "Interest": // TODO: Should be a 'reward'
					return null;
				default: throw new NotSupportedException($"{record.Type}");
			}
		}

		private bool HandleDepositAndWithdrawel(NexoRecord record, List<Model.Activity> activities)
		{
			var inFiat = fiat.Any(x => x.Symbol == record.InputCurrency);
			var outFiat = fiat.Any(x => x.Symbol == record.OutputCurrency);

			var inFiatCoin = fiatCoin.Any(x => x.Symbol == record.InputCurrency);
			var outFiatCoin = fiatCoin.Any(x => x.Symbol == record.OutputCurrency);

			var deposit = inFiat && outFiatCoin;
			var withdrawl = inFiatCoin && outFiat;

			if (!deposit && !withdrawl)
			{
				return false;
			}

			var refCode = $"Cash_Change_{record.DateTime}";
			var activity = new Model.Activity
			{
				Asset = null,
				Date = record.DateTime,
				Comment = $"Transaction Reference: [{refCode}]",
				Fee = null,
				Quantity = record.OutputAmount,
				ActivityType = deposit ? Model.ActivityType.CashDeposit : Model.ActivityType.CashWithdrawal,
				UnitPrice = new Model.Money(CurrencyHelper.ParseCurrency(deposit ? record.InputCurrency : record.OutputCurrency), 1, record.DateTime),
				ReferenceCode = refCode,
			};
			activities.Add(activity);
			return true;
		}
	}
}
