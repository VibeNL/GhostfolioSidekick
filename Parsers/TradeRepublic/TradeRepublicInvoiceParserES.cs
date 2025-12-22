using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1192:String literals should not be duplicated", Justification = "Seperate fields, just named the same in this language")]
	public class TradeRepublicInvoiceParserES(IPdfToWordsParser parsePDfToWords) : TradeRepublicInvoiceParserBase(parsePDfToWords)
	{
		protected override string[] HeaderKeywords => ["POSICIÓN", "CANTIDAD", "PRECIO", "IMPORTE"];
		protected override string StopWord => "RESERVA";

	}
}
