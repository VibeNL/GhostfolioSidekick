using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class BaaderBankRKKRecord
	{
		[Name("XXX-UMART")]
		public required string OrderType { get; set; }

		[Name("XXX-TEXT1")]
		public required string Symbol { get; set; }

		[Name("XXX-TEXT2")]
		public required string Isin { get; set; }

		[Name("XXX-SALDO")]
		[CultureInfo("nl-NL")]
		public decimal? UnitPrice { get; set; }

		[Name("XXX-WHG")]
		public required string Currency { get; set; }

		[Name("XXX-REFNR1")]
		public required string Reference { get; set; }

		[Name("XXX-VALUTA")]
		[Format("yyyyMMdd")]
		public DateOnly Date { get; set; }

		[Name("XXX-TEXT3")]
		public required string Quantity { get; set; }
	}
}
