using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.Bunq
{
	public class BunqRecord
	{
		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		[Format("yyyy-MM-dd")]
		public DateTime Date { get; set; }

		[Name("Interest Date")]
		public required string InterestDate { get; set; }

		[CultureInfo("nl-NL")]
		public decimal Amount { get; set; }

		public required string Name { get; set; }

		public required string Description { get; set; }
	}
}
