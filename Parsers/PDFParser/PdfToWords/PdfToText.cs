using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public class PdfToText : IPdfToText
	{
		public string GetText(string filePath)
		{
			var stringBuilder = new StringBuilder();

			using (var document = PdfDocument.Open(filePath))
			{
				foreach (Page page in document.GetPages())
				{
					stringBuilder.Append(ContentOrderTextExtractor.GetText(page));
				}
			}

			return stringBuilder.ToString();
		}
	}
}
