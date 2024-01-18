using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class ScalableCapitalWUMParser : RecordBaseImporter<BaaderBankWUMRecord>
	{
		public ScalableCapitalWUMParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(BaaderBankWUMRecord record, int rowNumber)
		{
			var date = record.Date.ToDateTime(record.Time);
			var currency = new Currency(record.Currency);
			switch (record.OrderType)
			{
				case "Verkauf":
					return [PartialActivity.CreateSell(currency, date, [record.Isin], Math.Abs(record.Quantity.GetValueOrDefault()), record.UnitPrice.GetValueOrDefault(), record.Reference)];
				case "Kauf":
					return [PartialActivity.CreateBuy(currency, date, [record.Isin], Math.Abs(record.Quantity.GetValueOrDefault()), record.UnitPrice.GetValueOrDefault(), record.Reference)];
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
				Delimiter = ";",
			};
		}
	}
}
