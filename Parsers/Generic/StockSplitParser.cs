using CsvHelper.Configuration;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class StockSplitParser : RecordBaseImporter<StockSplitRecord>
	{
		public StockSplitParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(StockSplitRecord record, int rowNumber)
		{
			// TODO What to do with this?
			return [];
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