using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.NIBC
{
	public class NIBCRecord
	{
		[Name("Boekhdk. datum")]
		[Format("dd-MM-yyyy")]
		public DateTime Date { get; set; }

		[Name("Beschrijving")]
		public required string Description { get; set; }

		[Name("Bedrag v/d verrichting")]
		[CultureInfo("nl-NL")]
		public decimal Amount { get; set; }

		[Name("Munt")]
		public required string Currency { get; set; }

		[Name("Ref. v/d verrichting")]
		public required string TransactionID { get; set; }
	}
}
