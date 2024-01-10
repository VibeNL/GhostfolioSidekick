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

		protected override async Task<IEnumerable<Activity>> ConvertOrders(BitvavoRecord record, Account account, IEnumerable<BitvavoRecord> allRecords)
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
				activity = new Activity(
					activityType,
					null,
					dateTime,
					1,
					new Money(CurrencyHelper.EUR, factor * record.Amount, dateTime),
					null,
					TransactionReferenceUtilities.GetComment(record.Transaction, record.Currency),
					record.Transaction);
			}
			else
			{
				var asset = await GetAsset(record.Currency, account);

				var fees = new List<Money>();

				if (record.Fee != null && CurrencyHelper.IsFiat(record.FeeCurrency))
				{
					fees.Add(new Money(record.FeeCurrency, record.Fee.GetValueOrDefault(0), dateTime));
				}

				var unitprice = new Money(CurrencyHelper.EUR, 0, dateTime);

				if (asset != null)
				{
					unitprice = await GetCorrectUnitPrice(new Money(CurrencyHelper.EUR, record.Price ?? 0, dateTime), asset, dateTime);
				}

				activity = new Activity(
					activityType,
					asset,
					dateTime,
					Math.Abs(record.Amount),
					unitprice,
					fees,
					TransactionReferenceUtilities.GetComment(record.Transaction, record.Currency),
					record.Transaction
					);
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
