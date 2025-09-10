using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class GenericRecord
	{
		public PartialActivityType ActivityType { get; set; }

		public string? Symbol { get; set; }

		[Optional]
		public string? ISIN { get; set; }

		[Optional]
		public string? Name { get; set; }

		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		public DateTime Date { get; set; }

		public required string Currency { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitPrice { get; set; }

		[Optional]
		public decimal? Fee { get; set; }

		[Optional]
		public decimal? Tax { get; set; }

		[Optional]
		public string? Id { get; set; }
	}
}
