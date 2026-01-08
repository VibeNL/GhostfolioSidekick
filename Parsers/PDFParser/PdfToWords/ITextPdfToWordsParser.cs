using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public class ITextPdfToWordsParser : IPdfToWordsParser
	{
		public virtual List<SingleWordToken> ParseTokens(string filePath)
		{
			List<SingleWordToken> words = [];

			using (var reader = new PdfReader(filePath))
			using (var document = new PdfDocument(reader))
			{
				for (int pageNum = 1; pageNum <= document.GetNumberOfPages(); pageNum++)
				{
					var page = document.GetPage(pageNum);
					words.AddRange(ParseWords(page, pageNum - 1)); // Convert to 0-based page index
				}
			}

			return words;
		}

		public virtual List<SingleWordToken> ParseTokensIgnoringFooter(string filePath, int footerHeightThreshold = 50)
		{
			var allTokens = ParseTokens(filePath);
			return PdfToWordsParser.FilterOutFooter(allTokens, footerHeightThreshold);
		}

		protected static IEnumerable<SingleWordToken> ParseWords(iText.Kernel.Pdf.PdfPage page, int pageIndex)
		{
			var strategy = new SimpleTextExtractionStrategy();
			var text = PdfTextExtractor.GetTextFromPage(page, strategy);
			
			var pageHeight = page.GetPageSize().GetHeight();
			var tokens = new List<SingleWordToken>();
			
			// Split text into words and create tokens
			var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			
			for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				var words = lines[lineIndex].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				int columnIndex = 0;
				
				foreach (var word in words)
				{
					if (!string.IsNullOrWhiteSpace(word))
					{
						// Estimate position based on line and word position
						int row = lineIndex * 12; // Approximate line height
						int column = columnIndex * 8; // Approximate character width
						
						tokens.Add(new SingleWordToken(word.Trim(), new Position(pageIndex, row, column)));
						columnIndex += word.Length + 1; // Add space
					}
				}
			}
			
			return tokens;
		}
	}
}