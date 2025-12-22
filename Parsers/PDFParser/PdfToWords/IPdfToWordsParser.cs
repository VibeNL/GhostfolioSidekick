namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public interface IPdfToWordsParser
	{
		List<SingleWordToken> ParseTokens(string filePath);
		
		/// <summary>
		/// Parses PDF tokens while filtering out footer content.
		/// </summary>
		/// <param name="filePath">Path to the PDF file</param>
		/// <param name="footerHeightThreshold">Distance from bottom of page to consider as footer area (default: 50)</param>
		/// <returns>List of tokens excluding footer content</returns>
		List<SingleWordToken> ParseTokensIgnoringFooter(string filePath, int footerHeightThreshold = 50);
	}
}
