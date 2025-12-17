using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{

	internal class TestPdfToWords(Dictionary<int, string> text) : IPdfToText
	{
		public Dictionary<int, string> Text { get; internal set; } = text;

		public string GetText(string filePath)
		{
			return string.Join(Environment.NewLine, Text.OrderBy(kv => kv.Key).Select(kv => kv.Value));
		}
	}
}