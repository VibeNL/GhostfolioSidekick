using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public class PdfToWordsParser : IPdfToWordsParser
	{
		public virtual List<SingleWordToken> ParseTokens(string filePath)
		{
			List<SingleWordToken> words = [];

			using (PdfDocument pdf = PdfDocument.Open(filePath))
			{
				foreach (var page in pdf.GetPages())
				{
					words.AddRange(ParseWords(page));
				}
			}

			return words;
		}

		protected static IEnumerable<SingleWordToken> ParseWords(Page page)
		{
			var tokens = new List<SingleWordToken>();
			var pageHeight = page.Height;
			foreach (var word in page.GetWords())
			{
				var bbox = word.BoundingBox;
				int row = (int)Math.Round(pageHeight - bbox.Bottom); // top to bottom
				int column = (int)Math.Round(bbox.Left);
				tokens.Add(new SingleWordToken(word.Text, new Position(page.Number - 1, row, column)));
			}

			return tokens;
		}

		// Backward-compatible path for tests that feed plain text.
		protected static IEnumerable<SingleWordToken> ParseWords(string text, int page)
		{
			var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
			var tokens = new List<SingleWordToken>();

			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i].ToCharArray();
				var word = new StringBuilder();
				var c = 0;

				for (int j = 0; j < line.Length; j++)
				{
					ProcessCharacter(line[j], word, ref c, j, tokens, page, i);
				}

				if (word.Length > 0)
				{
					tokens.Add(new SingleWordToken(word.ToString().Trim(), new Position(page, i, c)));
				}
			}

			return tokens;
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
