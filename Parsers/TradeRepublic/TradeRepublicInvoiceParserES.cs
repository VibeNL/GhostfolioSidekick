using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1192:String literals should not be duplicated", Justification = "Seperate fields, just named the same in this language")]
	public class TradeRepublicInvoiceParserES(IPdfToText parsePDfToWords) : TradeRepublicInvoiceParserBase(parsePDfToWords)
	{
		protected override bool CanParseRecords(string words)
		{
			throw new NotImplementedException();
		}

		protected override IEnumerable<PartialActivity> ParseRecords(string v)
		{
			throw new NotImplementedException();
		}
	}
}
