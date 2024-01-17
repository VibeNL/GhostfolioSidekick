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

		protected override Task<IEnumerable<PartialActivity>> ParseRow(T record, int rowNumber)
		{
			var recordDate = record.Date.ToDateTime(record.Time);

			var knownBalance = PartialActivity.CreateKnownBalance(new Currency(record.BalanceCurrency), recordDate, record.Balance);
			PartialActivity? partialActivity = null;

			var activityType = record.GetActivityType();

			var currencyRecord = new Currency(record.Mutation);
			var recordTotal = record.Total.GetValueOrDefault();

			switch (activityType)
			{
				case null:
				case ActivityType.Undefined:
					return Task.FromResult<IEnumerable<PartialActivity>>([knownBalance]);
				case ActivityType.Buy:
					partialActivity = PartialActivity.CreateBuy(
						currencyRecord,
						recordDate,
						record.ISIN!,
						record.GetQuantity(),
						new Money(currencyRecord, record.GetUnitPrice()),
						record.TransactionId!);
					break;
				case ActivityType.CashDeposit:
					partialActivity = PartialActivity.CreateCashDeposit(currencyRecord, recordDate, recordTotal);
					break;
				case ActivityType.CashWithdrawal:
					partialActivity = PartialActivity.CreateCashWithdrawal(currencyRecord, recordDate, recordTotal);
					break;
				case ActivityType.Dividend:
					partialActivity = PartialActivity.CreateDividend(currencyRecord, recordTotal, recordDate);
					break;
				case ActivityType.Fee:
					partialActivity = PartialActivity.CreateFee(currencyRecord, recordDate, recordTotal);
					break;
				case ActivityType.Tax:
					partialActivity = PartialActivity.CreateTax(currencyRecord, recordDate, recordTotal);
					break;
				case ActivityType.Gift:
					partialActivity = PartialActivity.CreateGift(currencyRecord, recordDate, recordTotal);
					break;
				case ActivityType.Interest:
					partialActivity = PartialActivity.CreateInterest(currencyRecord, recordDate, recordTotal);
					break;
				case ActivityType.Sell:
					partialActivity = PartialActivity.CreateSell(
						currencyRecord,
						recordDate,
						record.ISIN!,
						record.GetQuantity(),
						new Money(currencyRecord, record.GetUnitPrice()),
						record.TransactionId!);
					break;
				default:
					throw new NotSupportedException();
			}

			return Task.FromResult<IEnumerable<PartialActivity>>([knownBalance, partialActivity]);
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