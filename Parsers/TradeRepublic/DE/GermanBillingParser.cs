using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic.DE
{
	public static class GermanBillingParser
	{
		public static readonly string[] BillingHeaders = ["POSITION", "BETRAG"];
		public static readonly ColumnAlignment[] BillingColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Right];

		public static TableDefinition CreateBillingTableDefinition(string endMarker = "GESAMT", bool isRequired = false)
		{
			return BillingParserBase.CreateBillingTableDefinition(
				BillingHeaders,
				BillingColumnAlignment,
				endMarker,
				isRequired);
		}

		public static IEnumerable<PartialActivity> ParseBillingRecord(
			PdfTableRowColumns row,
			DateTime date,
			string transactionId,
			Func<string, decimal> parseDecimal)
		{
			return BillingParserBase.ParseBillingRecord(
				row,
				BillingHeaders,
				date,
				transactionId,
				parseDecimal,
				"Zwischensumme");
		}
	}
}
