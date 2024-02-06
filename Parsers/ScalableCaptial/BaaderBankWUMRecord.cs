using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class BaaderBankWUMRecord
	{
		[Name("XXX-BUDAT")]
		[Format("yyyyMMdd")]
		public DateOnly Date { get; set; }

		[Name("XXX-BUZEIT")]
		[Format("HHmmssFF")]
		public TimeOnly Time { get; set; }

		[Name("XXX-WPKURS")]
		[CultureInfo("nl-NL")]
		public decimal? UnitPrice { get; set; }

		[Name("XXX-WHGAB")]
		public required string Currency { get; set; }

		[Name("XXX-NW")]
		[CultureInfo("nl-NL")]
		public decimal? Quantity { get; set; }

		[Name("XXX-WPNR")]
		public required string Isin { get; set; }

		[Name("XXX-WPGART")]
		public required string OrderType { get; set; }

		[Name("XXX-EXTORDID")]
		public required string Reference { get; set; }
	}
}
