using CsvHelper;

namespace GhostfolioSidekick.Parsers.NIBC
{
	[CsvRecord]
	public partial class NIBCRecord
	{
		[CsvColumn("Boekhdk. datum")]
		[CsvConverter(typeof(DayFirstUtcDateTimeConverter))]
		public DateTime Date { get; set; }

		[CsvColumn("Beschrijving")]
		public required string Description { get; set; }

		[CsvColumn("Bedrag v/d verrichting")]
		[CsvCulture("nl-NL")]
		public decimal Amount { get; set; }

		[CsvColumn("Munt")]
		public required string Currency { get; set; }

		[CsvColumn("Ref. v/d verrichting")]
		public required string TransactionID { get; set; }
	}
}
