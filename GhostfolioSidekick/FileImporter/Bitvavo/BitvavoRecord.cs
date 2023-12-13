using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	[Delimiter(",")]
	public class BitvavoRecord
	{
		[Name("Transaction ID")]
		public string Transaction { get; set; }

		[Format("yyyy-MM-dd")]
		public DateOnly Date { get; set; }

		[Format("HH:mm:ss.fff", "HH:mm:ss")]
		public TimeOnly Time { get; set; }

		public string Type { get; set; }

		public string Currency { get; set; }

		public decimal Amount { get; set; }

		[Name("Price (EUR)")]
		public decimal? Price { get; set; }

		[Name("Fee currency")]
		public string FeeCurrency { get; set; }

		[Name("Fee amount")]
		public decimal? Fee { get; set; }

		[Name("Status")]
		public string Status { get; set; }
	}
}