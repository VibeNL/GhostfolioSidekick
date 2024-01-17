using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public abstract class DeGiroParserBase<T> : RecordBaseImporter<T> where T : DeGiroRecordBase
	{
		public DeGiroParserBase()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(T record, int rowNumber)
		{
			var recordDate = record.Date.ToDateTime(record.Time);

			var knownBalance = PartialActivity.CreateKnownBalance(new Currency(record.BalanceCurrency), recordDate, record.Balance, null);
			PartialActivity? partialActivity = null;

			var activityType = record.GetActivityType();

			var currencyRecord = new Currency(record.Mutation);
			var recordTotal = Math.Abs(record.Total.GetValueOrDefault());

			switch (activityType)
			{
				case null:
				case ActivityType.Undefined:
					return [knownBalance];
				case ActivityType.Buy:
					partialActivity = PartialActivity.CreateBuy(
						currencyRecord,
						recordDate,
						record.ISIN!,
						record.GetQuantity(),
						record.GetUnitPrice(),
						record.TransactionId);
					break;
				case ActivityType.CashDeposit:
					partialActivity = PartialActivity.CreateCashDeposit(currencyRecord, recordDate, recordTotal, record.TransactionId);
					break;
				case ActivityType.CashWithdrawal:
					partialActivity = PartialActivity.CreateCashWithdrawal(currencyRecord, recordDate, recordTotal, record.TransactionId);
					break;
				case ActivityType.Dividend:
					partialActivity = PartialActivity.CreateDividend(currencyRecord, recordDate, record.ISIN!, recordTotal, record.TransactionId);
					break;
				case ActivityType.Fee:
					partialActivity = PartialActivity.CreateFee(currencyRecord, recordDate, recordTotal, record.TransactionId);
					break;
				case ActivityType.Tax:
					partialActivity = PartialActivity.CreateTax(currencyRecord, recordDate, recordTotal, record.TransactionId);
					break;
				case ActivityType.Gift:
					partialActivity = PartialActivity.CreateGift(currencyRecord, recordDate, recordTotal, record.TransactionId);
					break;
				case ActivityType.Interest:
					partialActivity = PartialActivity.CreateInterest(currencyRecord, recordDate, recordTotal, record.TransactionId);
					break;
				case ActivityType.Sell:
					partialActivity = PartialActivity.CreateSell(
						currencyRecord,
						recordDate,
						record.ISIN!,
						record.GetQuantity(),
						record.GetUnitPrice(),
						record.TransactionId);
					break;
				default:
					throw new NotSupportedException();
			}

			return [knownBalance, partialActivity];
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