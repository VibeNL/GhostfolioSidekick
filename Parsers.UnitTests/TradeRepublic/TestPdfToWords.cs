using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public partial class TradeRepublicStatementParserNLTests
	{
		internal class TestPdfToWords : PdfToWordsParser
		{
			public TestPdfToWords(Dictionary<int, string> text)
			{
				Text = text;
			}

			public Dictionary<int, string> Text { get; internal set; }

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
}