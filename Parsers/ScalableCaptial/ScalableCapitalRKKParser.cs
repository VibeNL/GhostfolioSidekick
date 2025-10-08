using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class ScalableCapitalRKKParser(ICurrencyMapper currencyMapper) : RecordBaseImporter<BaaderBankRKKRecord>
	{
		protected override IEnumerable<PartialActivity> ParseRow(BaaderBankRKKRecord record, int rowNumber)
		{
			if (record.ExecutionDate == "0")
			{
				return [];
			}

			var date = DateTime.ParseExact(record.ExecutionDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

			var currency = currencyMapper.Map(record.Currency);
			if (record.OrderType == "Saldo")
			{
				return [PartialActivity.CreateKnownBalance(
					currencyMapper.Map(record.Currency),
					date,
					record.UnitPrice.GetValueOrDefault(0))];
			}

			if (record.OrderType == "Coupons/Dividende")
			{
				var quantity = decimal.Parse(record.Quantity.Replace("STK ", string.Empty), GetCultureForParsingNumbers());
				var unitPrice = record.UnitPrice.GetValueOrDefault() / quantity;

				return [PartialActivity.CreateDividend(
					currency,
					date,
					[PartialSymbolIdentifier.CreateStockAndETF(record.Isin.Replace("ISIN ", string.Empty))],
					quantity * unitPrice,
					new Money(currency, Math.Abs(record.UnitPrice.GetValueOrDefault(0))),
					record.Reference
					)];
			}

			if (record.Symbol == "ORDERGEBUEHR")
			{
				return [PartialActivity.CreateFee(
					currencyMapper.Map(record.Currency),
					date,
					Math.Abs(record.UnitPrice.GetValueOrDefault(0)),
					new Money(currency, Math.Abs(record.UnitPrice.GetValueOrDefault(0))),
					record.Reference)];
			}

			return [];
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";",
			};
		}

		private static CultureInfo GetCultureForParsingNumbers()
		{
			return new CultureInfo("en")
			{
				NumberFormat =
				{
					NumberDecimalSeparator = ","
				}
			};
		}
	}
}
