using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserDE(IPdfToWordsParser parsePDfToWords) : TradeRepublicInvoiceParserBase(parsePDfToWords)
	{
		protected override string[] HeaderKeywords => ["POSITION", "STÜCKZAHL", "PREIS", "BETRAG"];
		protected override string StopWord => "BUCHUNG";

	}
}
