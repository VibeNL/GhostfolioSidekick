using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{

	internal class TestPdfToWords(Dictionary<int, string> text) : PdfToWordsParser
	{
		public Dictionary<int, string> Text { get; internal set; } = text;

		public override List<SingleWordToken> ParseTokens(string filePath)
		{
			var lst = new List<SingleWordToken>();
			foreach (var item in Text)
			{
				lst.AddRange(ParseWords(item.Value, item.Key));
			}

			return lst;
		}
	}
}