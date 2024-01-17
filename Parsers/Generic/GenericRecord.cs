using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class GenericRecord
	{
		public ActivityType ActivityType { get; set; }

		public string? Symbol { get; set; }

		public DateTime Date { get; set; }

		public required string Currency { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitPrice { get; set; }

		public decimal? Fee { get; set; }

		[CsvHelper.Configuration.Attributes.Optional]
		public decimal? Tax { get; set; }

		[CsvHelper.Configuration.Attributes.Optional]
		public string? Id { get; set; }
	}
}
