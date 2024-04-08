using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class ScalableCapitalPrimeRecord
	{
		[Format("yyyy-MM-dd")]
		public DateOnly Date { get; set; }

		[Format("HHmmss")]
		public TimeOnly Time { get; set; }

		public required string Status { get; set; }

		public required string Reference { get; set; }

		public required string Description { get; set; }

		public required string AssetType { get; set; }

		public required string Type { get; set; }

		public required string Isin { get; set; }

		public required decimal Shares { get; set; }

		public required decimal Price { get; set; }

		public required decimal Amount { get; set; }

		public required decimal Fee { get; set; }

		public required decimal Tax { get; set; }

		public required string Currency { get; set; }
	}
}
