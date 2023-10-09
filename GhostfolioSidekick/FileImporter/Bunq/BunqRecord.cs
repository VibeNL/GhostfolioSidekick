using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.Bunq
{
	public class BunqRecord
	{
		public DateTime Date { get; set; }

		[Name("Interest Date")]
		public string InterestDate { get; set; }

		[CultureInfo("nl-NL")]
		public decimal Amount { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }
	}
}
