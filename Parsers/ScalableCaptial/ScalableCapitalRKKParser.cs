using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "<Pending>")]
	public class ScalableCapitalRKKParser : RecordBaseImporter<BaaderBankRKKRecord>
	{
		public ScalableCapitalRKKParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(BaaderBankRKKRecord record, int rowNumber)
		{
			if (record.ExecutionDate == "0")
			{
				return [];
			}

			var date = DateTime.ParseExact(record.ExecutionDate, "yyyyMMdd", null);

			var currency = new Currency(record.Currency);
			if (record.OrderType == "Saldo")
			{
				return [PartialActivity.CreateKnownBalance(new Currency(record.Currency), date, record.UnitPrice.GetValueOrDefault(0))];
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
					record.Reference
					)];
			}

			if (record.Symbol == "ORDERGEBUEHR")
			{
				return [PartialActivity.CreateFee(new Currency(record.Currency), date, Math.Abs(record.UnitPrice.GetValueOrDefault(0)), record.Reference)];
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
