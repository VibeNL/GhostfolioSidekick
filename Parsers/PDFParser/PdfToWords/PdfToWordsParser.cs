using Spire.Pdf.Texts;
using Spire.Pdf;
using System.Text;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public class PdfToWordsParser : IPdfToWordsParser
	{
		public virtual List<SingleWordToken> ParseTokens(string filePath)
		{
			List<SingleWordToken> words = new List<SingleWordToken>();

			// Load a PDF file
			using (PdfDocument document = new PdfDocument())
			{
				document.LoadFromFile(filePath);
				var i = 0;
				foreach (PdfPageBase page in document.Pages)
				{
					PdfTextExtractor textExtractor = new PdfTextExtractor(page);
					var text = textExtractor.ExtractText(new PdfTextExtractOptions());
					words.AddRange(ParseWords(text, i));
					i++;
				}
			}

			return words;
		}

		protected List<SingleWordToken> ParseWords(string text, int page)
		{
			// for each line
			var lines = text.Split([Environment.NewLine, "\n", "\r\n"], StringSplitOptions.None);
			var words = new List<SingleWordToken>();

			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i].ToCharArray();
				var word = new StringBuilder();
				var c = 0;
				for (int j = 0; j < line.Length; j++)
				{
					switch (line[j])
					{
						case ' ':
							if (word.Length > 0)
							{
								words.Add(new SingleWordToken(word.ToString(), new Position(page, i, c)));
							}

							word.Clear();
							break;
						default:
							word.Append(line[j]);
							if (c == 0)
							{
								c = j;
							}
							break;
					}
				}

				if (word.Length > 0)
				{
					words.Add(new SingleWordToken(word.ToString().Trim(), new Position(page, i, c)));
				}
			}

			return words;
		}
	}
}