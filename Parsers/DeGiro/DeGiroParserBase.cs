﻿using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public abstract class DeGiroParserBase<T> : RecordBaseImporter<T> where T : DeGiroRecordBase
	{
		private readonly ICurrencyMapper currencyMapper;

		protected DeGiroParserBase(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(T record, int rowNumber)
		{
			var recordDate = DateTime.SpecifyKind(record.Date.ToDateTime(record.Time), DateTimeKind.Utc);

			var knownBalance = PartialActivity.CreateKnownBalance(currencyMapper.Map(record.BalanceCurrency), recordDate, record.Balance, rowNumber);
			PartialActivity? partialActivity = null;

			var activityType = record.GetActivityType();

			var currencyRecord = currencyMapper.Map(record.Mutation);
			var recordTotal = Math.Abs(record.Total.GetValueOrDefault());

			record.SetGenerateTransactionIdIfEmpty(recordDate);

			switch (activityType)
			{
				case null:
				case ActivityType.Undefined:
					return [knownBalance];
				case ActivityType.Buy:
					partialActivity = PartialActivity.CreateBuy(
						record.GetCurrency(currencyMapper),
						recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)],
						record.GetQuantity(),
						record.GetUnitPrice(),
						record.TransactionId!);
					break;
				case ActivityType.CashDeposit:
					partialActivity = PartialActivity.CreateCashDeposit(currencyRecord, recordDate, recordTotal, record.TransactionId!);
					break;
				case ActivityType.CashWithdrawal:
					partialActivity = PartialActivity.CreateCashWithdrawal(currencyRecord, recordDate, recordTotal, record.TransactionId!);
					break;
				case ActivityType.Dividend:
					partialActivity = PartialActivity.CreateDividend(currencyRecord, recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)], recordTotal, record.TransactionId!);
					break;
				case ActivityType.Fee:
					partialActivity = PartialActivity.CreateFee(currencyRecord, recordDate, recordTotal, record.TransactionId!);
					break;
				case ActivityType.Tax:
					partialActivity = PartialActivity.CreateTax(currencyRecord, recordDate, recordTotal, record.TransactionId!);
					break;
				case ActivityType.Interest:
					partialActivity = PartialActivity.CreateInterest(currencyRecord, recordDate, recordTotal, record.Description, record.TransactionId!);
					break;
				case ActivityType.Sell:
					partialActivity = PartialActivity.CreateSell(
						record.GetCurrency(currencyMapper),
						recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)],
						record.GetQuantity(),
						record.GetUnitPrice(),
						record.TransactionId!);
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