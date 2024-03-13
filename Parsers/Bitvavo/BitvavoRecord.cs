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

		[Name("Price (EUR)")]
		public decimal? Price { get; set; }

		[Name("EUR received / paid")]
		public decimal? TotalTransactionAmount { get; set; }

		[Name("Fee currency")]
		public required string FeeCurrency { get; set; }

		[Name("Fee amount")]
		public decimal? Fee { get; set; }

		[Name("Status")]
		public required string Status { get; set; }
	}
}