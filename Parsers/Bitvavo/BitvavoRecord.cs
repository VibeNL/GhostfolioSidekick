using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.Bitvavo
{
	[Delimiter(",")]
	public class BitvavoRecord
	{
		[Name("Transaction ID")]
		public required string Transaction { get; set; }

		[Format("yyyy-MM-dd")]
		public DateOnly Date { get; set; }

		[Format("HH:mm:ss.fff", "HH:mm:ss")]
		public TimeOnly Time { get; set; }

		public required string Type { get; set; }

		public required string Currency { get; set; }

		public decimal Amount { get; set; }

		[Name("Quote Currency")]
		public required string UnitCurrency { get; set; }

		[Name("Quote Price")]
		public decimal? UnitPrice { get; set; }

		[Name("Received / Paid Currency")]
		public required string TotalTransactionCurrency { get; set; }

		[Name("Received / Paid Amount")]
		public decimal? TotalTransactionAmount { get; set; }

		[Name("Fee currency")]
		public required string FeeCurrency { get; set; }

		[Name("Fee amount")]
		public decimal? Fee { get; set; }

		[Name("Status")]
		public required string Status { get; set; }
	}
}