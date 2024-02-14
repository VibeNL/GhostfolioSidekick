using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Coinbase
{
	[Delimiter(",")]
	public class CoinbaseRecord
	{
		[DateTimeStyles(DateTimeStyles.AssumeUniversal)]
		[Format("yyyy-MM-ddTHH:mm:ssZ")]
		public DateTime Timestamp { get; set; }

		[Name("Transaction Type")]
		public required string Type { get; set; }

		public required string Asset { get; set; }

		[Name("Quantity Transacted")]
		[NumberStyles(NumberStyles.Number | NumberStyles.AllowExponent)]
		public decimal Quantity { get; set; }

		[Name("Spot Price Currency")]
		public required string Currency { get; set; }

		[Name("Spot Price at Transaction")]
		public decimal? Price { get; set; }

		[Name("Fees and/or Spread")]
		public decimal? Fee { get; set; }

		public required string Notes { get; set; }
	}
}