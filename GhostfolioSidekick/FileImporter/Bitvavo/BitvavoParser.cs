using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class BitvavoParser : CryptoRecordBaseImporter<BitvavoRecord>
	{
		public BitvavoParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(BitvavoRecord record, IEnumerable<BitvavoRecord> allRecords, Currency defaultCurrency)
		{
			if (record.Status != "Completed")
			{
				return Array.Empty<Activity>();
			}

			var activities = new List<Activity>();

			DateTime dateTime = record.Date.ToDateTime(record.Time);

			Activity activity;
			ActivityType activityType = MapType(record.Type);
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
				var asset = await GetAsset(record.Currency, account);

				activity = new Activity
				{
					Asset = asset,
					Date = dateTime,
					Comment = TransactionReferenceUtilities.GetComment(record.Transaction, record.Currency),
					Quantity = Math.Abs(record.Amount),
					ActivityType = activityType,
					UnitPrice = new Money(CurrencyHelper.EUR, record.Price ?? 0, dateTime),//TODO
					ReferenceCode = record.Transaction,
					Fees = new[] { new Money(record.FeeCurrency, record.Fee.GetValueOrDefault(0), dateTime) }
				};
			}

			activities.Add(activity);

			return activities;
		}

		private ActivityType MapType(string type)
		{
			switch (type)
			{
				case "buy":
					return ActivityType.Buy;
				case "sell":
					return ActivityType.Sell;
				case "staking":
					return ActivityType.StakingReward;
				case "withdrawal":
					return ActivityType.CashWithdrawal;
				case "deposit":
					return ActivityType.CashDeposit;
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
