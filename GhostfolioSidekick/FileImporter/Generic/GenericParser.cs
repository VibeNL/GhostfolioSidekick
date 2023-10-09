using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Generic
{
	public class GenericParser : RecordBaseImporter<GenericRecord>
	{
		public GenericParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Model.Activity>> ConvertOrders(GenericRecord record, Model.Account account, IEnumerable<GenericRecord> allRecords)
		{
			var asset = string.IsNullOrWhiteSpace(record.Symbol) ? null : await api.FindSymbolByISIN(record.Symbol);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{record.ActivityType}_{record.Symbol}_{record.Date.ToString("yyyy-MM-dd")}";
			}

			var order = new Model.Activity(
				record.ActivityType,
				asset,
				record.Date,
				record.Quantity,
				new Model.Money(CurrencyHelper.ParseCurrency(record.Currency), record.UnitPrice, record.Date),
				new Model.Money(CurrencyHelper.ParseCurrency(record.Currency), record.Fee ?? 0, record.Date),
				$"Transaction Reference: [{record.Id}]",
				record.Id
				);

			return new[] { order };
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
