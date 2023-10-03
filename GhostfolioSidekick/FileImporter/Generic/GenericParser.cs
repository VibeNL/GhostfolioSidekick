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

		protected override async Task<IEnumerable<Activity>> ConvertOrders(GenericRecord record, Account account, IEnumerable<GenericRecord> allRecords)
		{
			var asset = await api.FindSymbolByISIN(record.Symbol);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{record.OrderType}_{record.Symbol}_{record.Date.ToString("yyyy-MM-dd")}";
			}

			var order = new Activity
			{
				AccountId = account.Id,
				Asset = asset,
				Currency = record.Currency,
				Date = record.Date,
				Comment = $"Transaction Reference: [{record.Id}]",
				Fee = record.Fee ?? 0,
				FeeCurrency = record.Currency,
				Quantity = record.Quantity,
				Type = record.OrderType,
				UnitPrice = record.UnitPrice,
				ReferenceCode = record.Id,
			};

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
