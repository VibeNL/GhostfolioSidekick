using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserEN(IPdfToWordsParser parsePDfToWords) : TradeRepublicInvoiceParserBase(parsePDfToWords)
	{
		protected override string[] HeaderKeywords => ["POSITION", "QUANTITY", "PRICE", "AMOUNT"];

		protected override string StopWord => "BOOKING";

	}
}
