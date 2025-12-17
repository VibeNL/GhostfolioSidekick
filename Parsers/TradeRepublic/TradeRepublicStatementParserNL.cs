using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicStatementParserNL(IPdfToText parsePDfToWords) : PdfBaseParser(parsePDfToWords)
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
