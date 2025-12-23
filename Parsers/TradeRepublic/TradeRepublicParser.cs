using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.ISIN;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;
using System.Linq;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicParser(
		IPdfToWordsParser parsePDfToWords,
		IEnumerable<ITradeRepublicActivityParser> subParsers) : PdfBaseParser(parsePDfToWords)
	{
		protected override bool IgnoreFooter => true;

		protected override int FooterHeightThreshold => 50;

		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			var foundTradeRepublic = false;

			for (int i = 0; i < words.Count; i++)
			{
				if (IsCheckWords("TRADE REPUBLIC BANK GMBH", words, i, true))
				{
					foundTradeRepublic = true;
				}
			}

			return foundTradeRepublic && subParsers.Any(p => p.CanParseRecord(words));
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			foreach (var parser in subParsers.Where(parser => parser.CanParseRecord(words)))
			{
				var activities = parser.ParseRecords(words);
				if (activities.Count > 0)
				{
					return activities;
				}
			}

			return [];
		}
	}
}
