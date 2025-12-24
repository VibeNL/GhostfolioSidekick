using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class EnglishDividendInvoiceParser : DividendInvoiceParserBase
	{
		private readonly string[] _dividendHeaders = ["POSITION", "QUANTITY", "INCOME", "AMOUNT"];
		private readonly ColumnAlignment[] _dividendColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		private readonly string[] _dividendKeywords = ["Dividend", "with", "the", "ex-tag"];
		private readonly string[] _dateTokens = ["DATE"];
		
		private readonly EnglishBillingParserAdapter _billingParser = new();
		private readonly EnglishPositionParserAdapter _positionParser = new();

		protected override string[] DividendHeaders => _dividendHeaders;
		protected override ColumnAlignment[] DividendColumnAlignment => _dividendColumnAlignment;
		protected override string[] DividendKeywords => _dividendKeywords;
		protected override string[] DateTokens => _dateTokens;
		protected override IBillingParser BillingParser => _billingParser;
		protected override IPositionParser PositionParser => _positionParser;

		private class EnglishBillingParserAdapter : IBillingParser
		{
			public string[] BillingHeaders => EnglishBillingParser.BillingHeaders;

			public TableDefinition CreateBillingTableDefinition(string endMarker = "TOTAL", bool isRequired = false)
			{
				return EnglishBillingParser.CreateBillingTableDefinition(endMarker, isRequired);
			}

			public IEnumerable<PartialActivity> ParseBillingRecord(PdfTableRowColumns row, DateTime date, string transactionId, Func<string, decimal> parseDecimal)
			{
				return EnglishBillingParser.ParseBillingRecord(row, date, transactionId, parseDecimal);
			}
		}

		private class EnglishPositionParserAdapter : IPositionParser
		{
			public string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn)
			{
				return EnglishPositionParser.ExtractIsin(positionColumn);
			}
		}
	}
}
