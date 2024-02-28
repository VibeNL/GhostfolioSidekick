using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class StockSplitRecord
	{
		public required string Symbol { get; set; }

		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		public DateTime Date { get; set; }

		public int StockSplitFrom { get; set; }

		public int StockSplitTo { get; set; }
	}
}
