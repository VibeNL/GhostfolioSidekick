using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.ISIN;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public abstract class TradeRepublicInvoiceParserBase(IPdfToText parsePDfToWords) : PdfBaseParser(parsePDfToWords)
	{
		
	}
}
