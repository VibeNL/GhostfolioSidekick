using CsvHelper.Configuration.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Parsers.Bunq
{
	public class BunqRecord
	{
		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		[Format("yyyy-MM-dd")]
		public DateTime Date { get; set; }

		[ExcludeFromCodeCoverage]
		[Name("Interest Date")]
		public required string InterestDate { get; set; }

		[CultureInfo("nl-NL")]
		public decimal Amount { get; set; }

		public required string Name { get; set; }

		public required string Description { get; set; }
	}
}
