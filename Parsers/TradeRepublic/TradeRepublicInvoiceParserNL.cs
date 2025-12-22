using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserNL(IPdfToWordsParser parsePDfToWords) : TradeRepublicInvoiceParserBase(parsePDfToWords)
	{
		protected override string[] HeaderKeywords => ["POSITIE", "AANTAL", "PRIJS", "BEDRAG"];
		protected override string StopWord => "BOEKING";

	}
}
