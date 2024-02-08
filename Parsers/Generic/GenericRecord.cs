using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class GenericRecord
	{
		public ActivityType ActivityType { get; set; }

		public string? Symbol { get; set; }

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
