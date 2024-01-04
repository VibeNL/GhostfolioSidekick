using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.NIBC
{
	public class NIBCRecord
	{
		[Name("Boekhdk. datum")]
		[Format("dd-MM-yyyy")]
		public DateTime Date { get; set; }

		[Name("Beschrijving")]
		public string Description { get; set; }

		[Name("Bedrag v/d verrichting")]
		[CultureInfo("nl-NL")]
		public decimal Amount { get; set; }

		[Name("Munt")]
		public string Currency { get; set; }

		[Name("Ref. v/d verrichting")]
		public string TransactionID { get; set; }
	}
}
