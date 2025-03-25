using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroParser : RecordBaseImporter<DeGiroRecord>
	{
		private readonly Dictionary<string, bool> KnownHeaderCache = [];
		private readonly ICurrencyMapper currencyMapper;

		private static IDeGiroStrategy strategy = new DeGiroMultiStrategy(
				new DeGiroEnglishStrategy(),
				new DeGiroDutchStrategy(),
				new DeGiroPortugueseStrategy()
		);

		public DeGiroParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(DeGiroRecord record, int rowNumber)
		{
			var recordDate = DateTime.SpecifyKind(record.Date.ToDateTime(record.Time), DateTimeKind.Utc);

			var knownBalance = PartialActivity.CreateKnownBalance(
				currencyMapper.Map(record.BalanceCurrency),
				recordDate,
				record.Balance,
				rowNumber);
			PartialActivity? partialActivity;

			var activityType = strategy.GetActivityType(record);

			var currencyRecord = !string.IsNullOrWhiteSpace(record.Mutation) ? currencyMapper.Map(record.Mutation) : strategy.GetCurrency(record, currencyMapper);
			var recordTotal = Math.Abs(record.Total.GetValueOrDefault());

			strategy.SetGenerateTransactionIdIfEmpty(record, recordDate);

			switch (activityType)
			{
				case null:
				case PartialActivityType.Undefined:
					return [knownBalance];
				case PartialActivityType.Buy:
					partialActivity = PartialActivity.CreateBuy(
						strategy.GetCurrency(record, currencyMapper),
						recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)],
						strategy.GetQuantity(record),
						strategy.GetUnitPrice(record),
						new Money(currencyRecord, GetRecordTotal(recordTotal, strategy.GetQuantity(record), strategy.GetUnitPrice(record))),
						record.TransactionId!);
					break;
				case PartialActivityType.CashDeposit:
					partialActivity = PartialActivity.CreateCashDeposit(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.CashWithdrawal:
					partialActivity = PartialActivity.CreateCashWithdrawal(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Dividend:
					partialActivity = PartialActivity.CreateDividend(currencyRecord, recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)], recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Fee:
					partialActivity = PartialActivity.CreateFee(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Tax:
					partialActivity = PartialActivity.CreateTax(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Interest:
					partialActivity = PartialActivity.CreateInterest(currencyRecord, recordDate, recordTotal, record.Description, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Sell:
					partialActivity = PartialActivity.CreateSell(
						strategy.GetCurrency(record, currencyMapper),
						recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)],
						strategy.GetQuantity(record),
						strategy.GetUnitPrice(record),
						new Money(currencyRecord, GetRecordTotal(recordTotal, strategy.GetQuantity(record), strategy.GetUnitPrice(record))),
						record.TransactionId!);
					break;
				default:
					throw new NotSupportedException();
			}

			return [knownBalance, partialActivity];
		}

		private static decimal GetRecordTotal(decimal recordTotal, decimal quantity, decimal unitPrice)
		{
			if (recordTotal == 0)
			{
				recordTotal = Math.Abs(quantity * unitPrice);
			}

			return recordTotal;
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