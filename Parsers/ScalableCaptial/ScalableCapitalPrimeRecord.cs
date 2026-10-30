using CsvHelper;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	[ExcludeFromCodeCoverage]
	[CsvRecord]
	public partial class ScalableCapitalPrimeRecord
	{
		[CsvColumn("date")]
		[CsvFormat("yyyy-MM-dd")]
		public DateOnly Date { get; set; }

		[CsvColumn("time")]
		[CsvFormat("HH:mm:ss")]
		public TimeOnly Time { get; set; }

		[CsvColumn("status")]
		public required string Status { get; set; }

		[CsvColumn("reference")]
		public required string Reference { get; set; }

		[CsvColumn("description")]
		public required string Description { get; set; }

		[CsvColumn("assetType")]
		public required string AssetType { get; set; }

		[CsvColumn("type")]
		public required string Type { get; set; }

		[CsvColumn("isin")]
		public required string Isin { get; set; }

		[CsvColumn("shares")]
		[CsvCulture("de-DE")]
		public decimal? Shares { get; set; }

		[CsvColumn("price")]
		[CsvCulture("de-DE")]
		public decimal? Price { get; set; }

		[CsvColumn("amount")]
		[CsvCulture("de-DE")]
		public required decimal Amount { get; set; }

		[CsvColumn("fee")]
		[CsvCulture("de-DE")]
		public decimal? Fee { get; set; }

		[CsvColumn("tax")]
		[CsvCulture("de-DE")]
		public decimal? Tax { get; set; }

		[CsvColumn("currency")]
		public required string Currency { get; set; }
	}
}
