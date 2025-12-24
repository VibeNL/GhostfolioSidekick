using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public static class DutchBillingParser
	{
		public static readonly string[] BillingHeaders = ["POSITIE", "BEDRAG"];
		public static readonly ColumnAlignment[] BillingColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Right];

		public static TableDefinition CreateBillingTableDefinition(string endMarker = "TOTAAL", bool isRequired = false)
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
				"Subtotaal");
		}
	}
}