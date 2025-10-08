using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public class PdfToWordsParser : IPdfToWordsParser
	{
		public virtual List<SingleWordToken> ParseTokens(string filePath)
		{
			List<SingleWordToken> words = [];

			using (PdfReader reader = new(filePath))
			{
				PdfDocument pdfDoc = new(reader);
				for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
				{
					ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
					string currentText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
					words.AddRange(ParseWords(currentText, i - 1));
				}
			}

			return words;
		}

		protected static List<SingleWordToken> ParseWords(string text, int page)
		{
			var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
			var words = new List<SingleWordToken>();

			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i].ToCharArray();
				var word = new StringBuilder();
				var c = 0;
				
				for (int j = 0; j < line.Length; j++)
				{
					ProcessCharacter(line[j], word, ref c, j, words, page, i);
				}

				if (word.Length > 0)
				{
					words.Add(new SingleWordToken(word.ToString().Trim(), new Position(page, i, c)));
				}
			}

			return words;
		}

		private static void ProcessCharacter(char character, StringBuilder word, ref int c, int position, List<SingleWordToken> words, int page, int lineIndex)
		{
			switch (character)
			{
				case ' ':
					if (word.Length > 0)
					{
						words.Add(new SingleWordToken(word.ToString(), new Position(page, lineIndex, c)));
					}
					word.Clear();
					break;
				default:
					word.Append(character);
					if (c == 0)
					{
						c = position;
					}
					break;
			}
		}
	}
}
