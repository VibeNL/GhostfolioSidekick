namespace GhostfolioSidekick.FileImporter.Generic
{
	public class GenericRecord
	{
		public Model.ActivityType ActivityType { get; set; }

		public string? Symbol { get; set; }

		public DateTime Date { get; set; }

		public string Currency { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitPrice { get; set; }

		public decimal? Fee { get; set; }

		[CsvHelper.Configuration.Attributes.Optional]
		public string? Id { get; set; }
	}
}
