using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public interface ITradeRepublicActivityParser
	{
		bool CanParseRecord(string filename, List<SingleWordToken> words);

		List<PartialActivity> ParseRecords(string filename, List<SingleWordToken> words);
	}
}