using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class DutchDividendInvoiceParser : DividendInvoiceParserBase
	{
		private readonly string[] _dividendHeaders = ["POSITIE", "AANTAL", "OPBRENGST", "BEDRAG"];
		private readonly ColumnAlignment[] _dividendColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		private readonly string[] _dividendKeywords = ["Cash", "dividend", "met", "de", "Ex-Datum"];
		private readonly string[] _exTagKeywords = ["ex-tag"];
		
		private readonly DutchBillingParserAdapter _billingParser = new();
		private readonly DutchPositionParserAdapter _positionParser = new();

		protected override string[] DividendHeaders => _dividendHeaders;
		protected override ColumnAlignment[] DividendColumnAlignment => _dividendColumnAlignment;
		protected override string[] DividendKeywords => _dividendKeywords;
		protected override string[] ExTagKeywords => _exTagKeywords;
		protected override IBillingParser BillingParser => _billingParser;
		protected override IPositionParser PositionParser => _positionParser;

		private class DutchBillingParserAdapter : IBillingParser
		{
			public string[] BillingHeaders => DutchBillingParser.BillingHeaders;

			public TableDefinition CreateBillingTableDefinition(string endMarker = "TOTAAL", bool isRequired = false)
			{
				return DutchBillingParser.CreateBillingTableDefinition(endMarker, isRequired);
			}

			public IEnumerable<PartialActivity> ParseBillingRecord(PdfTableRowColumns row, DateTime date, string transactionId, Func<string, decimal> parseDecimal)
			{
				return DutchBillingParser.ParseBillingRecord(row, date, transactionId, parseDecimal);
			}
		}

		private class DutchPositionParserAdapter : IPositionParser
		{
			public string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn)
			{
				return DutchPositionParser.ExtractIsin(positionColumn);
			}
		}
	}
}