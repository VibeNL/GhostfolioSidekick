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

		public virtual List<SingleWordToken> ParseTokensIgnoringFooter(string filePath, int footerHeightThreshold = 50)
		{
			var allTokens = ParseTokens(filePath);
			return FilterOutFooter(allTokens, footerHeightThreshold);
		}

		/// <summary>
		/// Filters out tokens that appear in the footer area of pages.
		/// The footer is determined by tokens whose row position is within the specified threshold
		/// from the bottom of each page.
		/// </summary>
		/// <param name="tokens">All tokens from the PDF</param>
		/// <param name="footerHeightThreshold">Distance from bottom of page to consider as footer area</param>
		/// <returns>Filtered tokens excluding footer content</returns>
		public static List<SingleWordToken> FilterOutFooter(List<SingleWordToken> tokens, int footerHeightThreshold = 50)
		{
			if (tokens.Count == 0)
			{
				return tokens;
			}

			// Group tokens by page to handle each page separately
			var tokensByPage = tokens
				.Where(t => t.BoundingBox != null)
				.GroupBy(t => t.BoundingBox!.Page)
				.ToDictionary(g => g.Key, g => g.ToList());

			var filteredTokens = new List<SingleWordToken>();

			// Add tokens without bounding box (shouldn't happen in normal PDF parsing, but handle gracefully)
			filteredTokens.AddRange(tokens.Where(t => t.BoundingBox == null));

			foreach (var pageGroup in tokensByPage)
			{
				var pageTokens = pageGroup.Value;
				if (pageTokens.Count == 0)
				{
					continue;
				}

				// Find the maximum row position on this page (bottom of page)
				var maxRow = pageTokens.Max(t => t.BoundingBox!.Row);
				
				// Filter out tokens that are within the footer threshold from the bottom
				// Token is in footer if: maxRow - tokenRow <= footerHeightThreshold
				var filteredPageTokens = pageTokens
					.Where(t => (maxRow - t.BoundingBox!.Row) > footerHeightThreshold)
					.ToList();

				filteredTokens.AddRange(filteredPageTokens);
			}

			return filteredTokens;
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
