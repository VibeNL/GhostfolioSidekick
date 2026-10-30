using CsvHelper;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Parsers.Bunq
{
	[CsvRecord]
	public partial class BunqRecord
	{
		[CsvConverter(typeof(UtcDateTimeConverter))]
		public DateTime Date { get; set; }

		[ExcludeFromCodeCoverage]
		[CsvColumn("Interest Date")]
		public required string InterestDate { get; set; }

		[CsvCulture("nl-NL")]
		public decimal Amount { get; set; }

		public required string Name { get; set; }

		public required string Description { get; set; }
	}
}
