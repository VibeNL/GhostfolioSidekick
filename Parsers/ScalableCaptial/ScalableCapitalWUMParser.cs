using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "<Pending>")]
	public class ScalableCapitalWUMParser : RecordBaseImporter<BaaderBankWUMRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

		public ScalableCapitalWUMParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(BaaderBankWUMRecord record, int rowNumber)
		{
			var date = DateTime.SpecifyKind(record.Date.ToDateTime(record.Time), DateTimeKind.Utc);
			var currency = currencyMapper.Map(record.Currency);
			switch (record.OrderType)
			{
				case "Verkauf":
					return [PartialActivity.CreateSell(currency, date,
						[PartialSymbolIdentifier.CreateStockAndETF(record.Isin)], Math.Abs(record.Quantity.GetValueOrDefault()), record.UnitPrice.GetValueOrDefault(), record.Reference)];
				case "Kauf":
					return [PartialActivity.CreateBuy(currency, date,
						[PartialSymbolIdentifier.CreateStockAndETF(record.Isin)], Math.Abs(record.Quantity.GetValueOrDefault()), record.UnitPrice.GetValueOrDefault(), record.Reference)];
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
