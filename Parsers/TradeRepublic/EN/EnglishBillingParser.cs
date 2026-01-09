using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{
	public static class EnglishBillingParser
	{
		public static readonly string[] BillingHeaders = ["POSITION", "AMOUNT"];
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
