using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class ScalableCapitalWUMParser(ICurrencyMapper currencyMapper) : RecordBaseImporter<BaaderBankWUMRecord>
	{
		protected override IEnumerable<PartialActivity> ParseRow(BaaderBankWUMRecord record, int rowNumber)
		{
			var date = record.Date.ToDateTime(record.Time, DateTimeKind.Utc);
			var currency = currencyMapper.Map(record.Currency);
			return record.OrderType switch
			{
				"Verkauf" => [PartialActivity.CreateSell(
						currency,
						date,
						[PartialSymbolIdentifier.CreateStockAndETF(record.Isin)],
						Math.Abs(record.Quantity.GetValueOrDefault()),
						new Money(currency, record.UnitPrice.GetValueOrDefault()),
						new Money(currency, record.TotalPrice.GetValueOrDefault()),
						record.Reference)],
				"Kauf" => [PartialActivity.CreateBuy(
						currency,
						date,
						[PartialSymbolIdentifier.CreateStockAndETF(record.Isin)],
						Math.Abs(record.Quantity.GetValueOrDefault()),
						new Money(currency, record.UnitPrice.GetValueOrDefault()),
						new Money(currency, record.TotalPrice.GetValueOrDefault()),
						record.Reference)],
				_ => throw new NotSupportedException(),
			};
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
	}
}
