using CsvHelper;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Trine
{
	[CsvRecord]
	public partial class TrineRecord
	{
		[CsvConverter(typeof(UtcNullableDateTimeConverter))]
		public DateTime? Date { get; set; }

		public string? Loan { get; set; }

		public int? InvestmentId { get; set; }

		public required string Type { get; set; }

		public decimal AvailableBalanceChange { get; set; }

		public decimal OutstandingPortfolioChange { get; set; }

		public decimal? RepaidCapital { get; set; }

		public decimal? RepaidInterest { get; set; }

		public decimal? LateFee { get; set; }

		public decimal AvailableBalance { get; set; }

		public decimal OutstandingPortfolio { get; set; }

		public decimal Total { get; set; }
	}
}
