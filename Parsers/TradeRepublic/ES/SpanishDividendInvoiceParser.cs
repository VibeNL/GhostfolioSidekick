using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{
	public class SpanishDividendInvoiceParser : DividendInvoiceParserBase
	{
		private readonly string[] _dividendHeaders = ["POSICIÓN", "CANTIDAD", "RENDIMIENTO", "CANTIDAD"];
		private readonly ColumnAlignment[] _dividendColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		private readonly string[] _dividendKeywords = ["Dividendo", "en", "efectivo", "con", "Ex-Date"];
		private readonly string[] _dateTokens = ["FECHA"];
		private readonly string _billingEndMarker = "TOTAL";
		
		private readonly SpanishBillingParserAdapter _billingParser = new();
		private readonly SpanishPositionParserAdapter _positionParser = new();

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		protected override string[] DividendHeaders => _dividendHeaders;
		protected override ColumnAlignment[] DividendColumnAlignment => _dividendColumnAlignment;
		protected override string[] DividendKeywords => _dividendKeywords;
		protected override string[] DateTokens => _dateTokens;
		protected override string BillingEndMarker => _billingEndMarker;
		protected override IBillingParser BillingParser => _billingParser;
		protected override IPositionParser PositionParser => _positionParser;

		private class SpanishBillingParserAdapter : IBillingParser
		{
			public string[] BillingHeaders => SpanishBillingParser.BillingHeaders;

			public TableDefinition CreateBillingTableDefinition(string endMarker, bool isRequired = false)
			{
				return SpanishBillingParser.CreateBillingTableDefinition(endMarker, isRequired);
			}

			public IEnumerable<PartialActivity> ParseBillingRecord(PdfTableRowColumns row, DateTime date, string transactionId, Func<string, decimal> parseDecimal)
			{
				return SpanishBillingParser.ParseBillingRecord(row, date, transactionId, parseDecimal);
			}
		}

		private class SpanishPositionParserAdapter : IPositionParser
		{
			public string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn)
			{
				return ISINParser.ExtractIsin(positionColumn);
			}
		}
	}
}
