using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class BitvavoParser : CryptoRecordBaseImporter<BitvavoRecord>
	{
		public BitvavoParser(
			IApplicationSettings applicationSettings,
			IGhostfolioAPI api) : base(applicationSettings.ConfigurationInstance, api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(BitvavoRecord record, IEnumerable<BitvavoRecord> allRecords, Balance accountBalance)
		{
			if (record.Status != "Completed" && record.Status != "Distributed")
			{
				return Array.Empty<Activity>();
			}

			var activities = new List<Activity>();

			DateTime dateTime = record.Date.ToDateTime(record.Time);

			Activity activity;
			ActivityType activityType = MapType(record);
			if (activityType == ActivityType.CashDeposit || activityType == ActivityType.CashWithdrawal)
			{
				var factor = activityType == ActivityType.CashWithdrawal ? -1 : 1;
				activity = new Activity
				{
					Asset = null,
					Date = dateTime,
					Comment = TransactionReferenceUtilities.GetComment(record.Transaction, record.Currency),
					Quantity = 1,
					ActivityType = activityType,
					UnitPrice = new Money(CurrencyHelper.EUR, factor * record.Amount, dateTime),
					ReferenceCode = record.Transaction,
				};
			}
			else
			{
				var asset = await GetAsset(record.Currency, accountBalance.Currency);

				var fees = new List<Money>();

				if (record.Fee != null && CurrencyHelper.IsFiat(record.FeeCurrency))
				{
					fees.Add(new Money(record.FeeCurrency, record.Fee.GetValueOrDefault(0), dateTime));
				}

				activity = new Activity
				{
					Asset = asset,
					Date = dateTime,
					Comment = TransactionReferenceUtilities.GetComment(record.Transaction, record.Currency),
					Quantity = Math.Abs(record.Amount),
					ActivityType = activityType,
					UnitPrice = await GetCorrectUnitPrice(new Money(CurrencyHelper.EUR, record.Price ?? 0, dateTime), asset, dateTime),
					ReferenceCode = record.Transaction,
					Fees = fees
				};
			}

			activities.Add(activity);

			return activities;
		}

		private ActivityType MapType(BitvavoRecord record)
		{
			var isFiat = CurrencyHelper.IsFiat(record.Currency);
			switch (record.Type)
			{
				case "buy":
					return ActivityType.Buy;
				case "sell":
					return ActivityType.Sell;
				case "staking":
					return ActivityType.StakingReward;
				case "withdrawal":
					return isFiat ? ActivityType.CashWithdrawal : ActivityType.Send;
				case "deposit":
					return isFiat ? ActivityType.CashDeposit : ActivityType.Receive;
				case "rebate":
					return ActivityType.CashDeposit;
				case "affiliate":
					return ActivityType.CashDeposit;
				default:
					throw new NotSupportedException();
			}
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
