using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.DE
{
	public class GermanDividendInvoiceParser : DividendInvoiceParserBase
	{
		private readonly string[] _dividendHeaders = ["POSITION", "ANZAHL", "ERTRAG", "BETRAG"];
		private readonly ColumnAlignment[] _dividendColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		private readonly string[] _dividendKeywords = ["Dividende", "mit"];
		private readonly string[] _dateTokens = ["DATUM"];
		private readonly string _billingEndMarker = "GESAMT";
		
		private readonly GermanBillingParserAdapter _billingParser = new();
		private readonly GermanPositionParserAdapter _positionParser = new();

		protected override string[] DividendHeaders => _dividendHeaders;
		protected override ColumnAlignment[] DividendColumnAlignment => _dividendColumnAlignment;
		protected override string[] DividendKeywords => _dividendKeywords;
		protected override string[] DateTokens => _dateTokens;
		protected override string BillingEndMarker => _billingEndMarker;
		protected override IBillingParser BillingParser => _billingParser;
		protected override IPositionParser PositionParser => _positionParser;

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		private class GermanBillingParserAdapter : IBillingParser
		{
			public string[] BillingHeaders => GermanBillingParser.BillingHeaders;

			public TableDefinition CreateBillingTableDefinition(string endMarker, bool isRequired = false)
			{
				return GermanBillingParser.CreateBillingTableDefinition(endMarker, isRequired);
			}

			public IEnumerable<PartialActivity> ParseBillingRecord(PdfTableRowColumns row, DateTime date, string transactionId, Func<string, decimal> parseDecimal)
			{
				return GermanBillingParser.ParseBillingRecord(row, date, transactionId, parseDecimal);
			}
		}

		private class GermanPositionParserAdapter : IPositionParser
		{
			public string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn)
			{
				return ISINParser.ExtractIsin(positionColumn);
			}
		}
	}
}