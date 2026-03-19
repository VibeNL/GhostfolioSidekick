using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroParser(ICurrencyMapper currencyMapper) : RecordBaseImporter<DeGiroRecord>
	{
		private static readonly DeGiroMultiStrategy strategy = new(
				new DeGiroEnglishStrategy(),
				new DeGiroDutchStrategy(),
				new DeGiroPortugueseStrategy()
		);

		protected override IEnumerable<PartialActivity> ParseRow(DeGiroRecord record, int rowNumber)
		{
			var recordDate = record.Date.ToDateTime(record.Time, DateTimeKind.Utc);

			var knownBalance = PartialActivity.CreateKnownBalance(
				currencyMapper.Map(record.BalanceCurrency),
				recordDate,
				GetNumber(record.Balance),
				rowNumber);
			PartialActivity? partialActivity;

			var activityType = strategy.GetActivityType(record);

			var currencyRecord = !string.IsNullOrWhiteSpace(record.Mutation) ? currencyMapper.Map(record.Mutation) : strategy.GetCurrency(record, currencyMapper);
			var recordTotal = Math.Abs(GetNumber(record.Total));

			strategy.SetGenerateTransactionIdIfEmpty(record, recordDate);

			switch (activityType)
			{
				case null:
				case PartialActivityType.Undefined:
					return [knownBalance];
				case PartialActivityType.Buy:
					partialActivity = !string.IsNullOrWhiteSpace(record.ISIN)
						? PartialActivity.CreateBuy(
							strategy.GetCurrency(record, currencyMapper),
							recordDate,
							new[] { PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!, Currency.EUR) },
							strategy.GetQuantity(record),
							new Money(strategy.GetCurrency(record, currencyMapper), strategy.GetUnitPrice(record)),
							new Money(currencyRecord, GetRecordTotal(recordTotal, strategy.GetQuantity(record), strategy.GetUnitPrice(record))),
							record.TransactionId!)
						: null;
					break;
				case PartialActivityType.CashDeposit:
					partialActivity = PartialActivity.CreateCashDeposit(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.CashWithdrawal:
					partialActivity = PartialActivity.CreateCashWithdrawal(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Dividend:
					partialActivity = !string.IsNullOrWhiteSpace(record.ISIN)
						? PartialActivity.CreateDividend(currencyRecord, recordDate,
							new[] { PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!, Currency.EUR) }, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!)
						: null;
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
					partialActivity = !string.IsNullOrWhiteSpace(record.ISIN)
						 ? PartialActivity.CreateSell(
							 strategy.GetCurrency(record, currencyMapper),
							 recordDate,
							 new[] { PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!, Currency.EUR) },
							 strategy.GetQuantity(record),
							 new Money(strategy.GetCurrency(record, currencyMapper), strategy.GetUnitPrice(record)),
							 new Money(currencyRecord, GetRecordTotal(recordTotal, strategy.GetQuantity(record), strategy.GetUnitPrice(record))),
							 record.TransactionId!)
						 : null;
					break;
				default:
					throw new NotSupportedException();
			}

			return new[] { knownBalance, partialActivity }
						.Where(x => x is not null)
						.Cast<PartialActivity>();
		}

		private static decimal GetNumber(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}

			// If the last seperator is a comma, replace it with a dot
			if (text.LastIndexOf(',') > text.LastIndexOf('.'))
			{
				text = text.Replace(',', '.');
			}

			if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}

			return 0;
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