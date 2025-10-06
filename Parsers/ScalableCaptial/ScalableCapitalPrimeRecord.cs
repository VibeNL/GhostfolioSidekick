using CsvHelper.Configuration.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	[ExcludeFromCodeCoverage]
	public class ScalableCapitalPrimeRecord
	{
		[Name("date")]
		[Format("yyyy-MM-dd")]
		public DateOnly Date { get; set; }

		[Name("time")]
		[Format("HH:mm:ss")]
		public TimeOnly Time { get; set; }

		[Name("status")]
		public required string Status { get; set; }

		[Name("reference")]
		public required string Reference { get; set; }

		[Name("description")]
		public required string Description { get; set; }

		[Name("assetType")]
		public required string AssetType { get; set; }

		[Name("type")]
		public required string Type { get; set; }

		[Name("isin")]
		public required string Isin { get; set; }

		[Name("shares")]
		[CultureInfo("de-DE")]
		public decimal? Shares { get; set; }

		[Name("price")]
		[CultureInfo("de-DE")]
		public decimal? Price { get; set; }

		[Name("amount")]
		[CultureInfo("de-DE")]
		public required decimal Amount { get; set; }

		[Name("fee")]
		[CultureInfo("de-DE")]
		public decimal? Fee { get; set; }

		[Name("tax")]
		[CultureInfo("de-DE")]
		public decimal? Tax { get; set; }

		[Name("currency")]
		public required string Currency { get; set; }
	}
}
