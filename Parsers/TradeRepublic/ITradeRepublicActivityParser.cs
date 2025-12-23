using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public interface ITradeRepublicActivityParser
	{
		bool CanParseRecord(List<SingleWordToken> words);

		List<PartialActivity> ParseRecords(List<SingleWordToken> words);
	}
}