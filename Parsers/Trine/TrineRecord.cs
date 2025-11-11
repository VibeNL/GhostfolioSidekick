using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Trine
{
	public class TrineRecord
	{
		[DateTimeStyles(DateTimeStyles.AssumeUniversal)]
		public DateTime Date { get; set; }

		public string? Loan { get; set; }

		public int? InvestmentId { get; set; }

		public required string Type { get; set; }

		public int AvailableBalanceChange { get; set; }

		public int OutstandingPortfolioChange { get; set; }

		public int? RepaidCapital { get; set; }

		public int? RepaidInterest { get; set; }

		public int? LateFee { get; set; }

		public int AvailableBalance { get; set; }

		public int OutstandingPortfolio { get; set; }

		public int Total { get; set; }
		}
}
