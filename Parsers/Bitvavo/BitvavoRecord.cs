using CsvHelper;

namespace GhostfolioSidekick.Parsers.Bitvavo
{
	[CsvRecord]
	public partial class BitvavoRecord
	{
		[CsvColumn("Transaction ID")]
		public required string Transaction { get; set; }

		[CsvFormat("yyyy-MM-dd")]
		public DateOnly Date { get; set; }

		[CsvFormat("HH:mm:ss.fff", "HH:mm:ss")]
		public TimeOnly Time { get; set; }

		public required string Type { get; set; }

		public required string Currency { get; set; }

		public decimal Amount { get; set; }

		[CsvColumn("Quote Currency")]
		public required string UnitCurrency { get; set; }

		[CsvColumn("Quote Price")]
		public decimal? UnitPrice { get; set; }

		[CsvColumn("Received / Paid Currency")]
		public required string TotalTransactionCurrency { get; set; }

		[CsvColumn("Received / Paid Amount")]
		public decimal? TotalTransactionAmount { get; set; }

		[CsvColumn("Fee currency")]
		public required string FeeCurrency { get; set; }

		[CsvColumn("Fee amount")]
		public decimal? Fee { get; set; }

		[CsvColumn("Status")]
		public required string Status { get; set; }
	}
}