using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic.ES
{
	public static class SpanishBillingParser
	{
		// Original text: "POSICIÓN", "IMPORTE"
		public static readonly string[] BillingHeaders = ["POSICI\u00d3N", "IMPORTE"];
		public static readonly ColumnAlignment[] BillingColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Right];

		public static TableDefinition CreateBillingTableDefinition(string endMarker = "TOTAL", bool isRequired = false)
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
				"Subtotal");
		}
	}
}
