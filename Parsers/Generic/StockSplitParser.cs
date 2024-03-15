using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class StockSplitParser : CSVBaseImporter<StockSplitRecord>
	{
		public StockSplitParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(StockSplitRecord record, int rowNumber)
		{
					yield return PartialActivity.CreateStockSplit(record.Date,
						[PartialSymbolIdentifier.CreateGeneric(record.Symbol!)], 
						record.StockSplitFrom, 
						record.StockSplitTo,
						$"{PartialActivityType.StockSplit}_{record.Symbol}_{record.Date.ToInvariantDateOnlyString()}_{record.StockSplitFrom.ToString(CultureInfo.InvariantCulture)}_{record.StockSplitTo.ToString(CultureInfo.InvariantCulture)}");
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
