using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Generic
{
	public class GenericParser : RecordBaseImporter<GenericRecord>
	{
		public GenericParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(GenericRecord record, IEnumerable<GenericRecord> allRecords, Currency defaultCurrency)
		{
			var asset = string.IsNullOrWhiteSpace(record.Symbol) ? null : await api.FindSymbolByIdentifier(
				record.Symbol,
				CurrencyHelper.ParseCurrency(record.Currency) ?? defaultCurrency,
				null,
				null);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{record.ActivityType}_{record.Symbol}_{record.Date.ToInvariantDateOnlyString()}_{record.Quantity.ToString(CultureInfo.InvariantCulture)}_{record.Currency}_{record.Fee?.ToString(CultureInfo.InvariantCulture)}";
			}

			var order = new Activity(
				record.ActivityType,
				asset,
				record.Date,
				record.Quantity,
				new Money(CurrencyHelper.ParseCurrency(record.Currency), record.UnitPrice, record.Date),
				new[] { new Money(CurrencyHelper.ParseCurrency(record.Currency), record.Fee ?? 0, record.Date) },
				TransactionReferenceUtilities.GetComment(record.Id, record.Symbol),
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
