namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public interface IPdfToWordsParser
	{
		List<SingleWordToken> ParseTokens(string filePath);
	}
}
