using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{
	public class EnglishDividendInvoiceParser : DividendInvoiceParserBase
	{
		private readonly string[] _dividendHeaders = ["POSITION", "QUANTITY", "INCOME", "AMOUNT"];
		private readonly ColumnAlignment[] _dividendColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		private readonly string[] _dividendKeywords = ["Dividend", "with", "the", "ex-tag"];
		private readonly string[] _dateTokens = ["DATE"];
		private readonly string _billingEndMarker = "TOTAL";
		
		private readonly EnglishBillingParserAdapter _billingParser = new();
		private readonly EnglishPositionParserAdapter _positionParser = new();

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		protected override string[] DividendHeaders => _dividendHeaders;
		protected override ColumnAlignment[] DividendColumnAlignment => _dividendColumnAlignment;
		protected override string[] DividendKeywords => _dividendKeywords;
		protected override string[] DateTokens => _dateTokens;
		protected override string BillingEndMarker => _billingEndMarker;
		protected override IBillingParser BillingParser => _billingParser;
		protected override IPositionParser PositionParser => _positionParser;

		private sealed class EnglishBillingParserAdapter : IBillingParser
		{
			public string[] BillingHeaders => EnglishBillingParser.BillingHeaders;

			public TableDefinition CreateBillingTableDefinition(string endMarker, bool isRequired = false)
			{
				return EnglishBillingParser.CreateBillingTableDefinition(endMarker, isRequired);
			}

			public IEnumerable<PartialActivity> ParseBillingRecord(PdfTableRowColumns row, DateTime date, string transactionId, Func<string, decimal> parseDecimal)
			{
				return EnglishBillingParser.ParseBillingRecord(row, date, transactionId, parseDecimal);
			}
		}

		private sealed class EnglishPositionParserAdapter : IPositionParser
		{
			public string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn)
			{
				return ISINParser.ExtractIsin(positionColumn);
			}
		}
	}
}
