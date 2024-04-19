using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;
using System.Linq;
using UglyToad.PdfPig.Fonts.Standard14Fonts;

namespace UtilitiesCLI
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello, World!");

			var sourceFile = args[0];
			var targetFile = args[1];
			var forbiddenwords = args.Skip(2).ToList();

			using (PdfDocument document = PdfDocument.Open(sourceFile))
			{
				PdfDocumentBuilder builder = new PdfDocumentBuilder();
				// Fonts must be registered with the document builder prior to use to prevent duplication.
				PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.TimesRoman);

				for (var i = 0; i < document.NumberOfPages; i++)
				{
					var sourcePage = document.GetPage(i + 1);
					var targetPage = builder.AddPage(sourcePage.Width, sourcePage.Height);
					foreach (var word in from word in sourcePage.GetWords()
										 select word)
					{
						targetPage.AddText(
							ReplaceText(forbiddenwords, word.Text),
							(decimal)word.Letters[0].FontSize,
							new UglyToad.PdfPig.Core.PdfPoint(
								word.BoundingBox.Left, word.BoundingBox.Bottom),
								font);
					}
				}

				byte[] documentBytes = builder.Build();
				File.WriteAllBytes(targetFile, documentBytes);
			}

		}

		private static string ReplaceText(List<string> forbiddenwords, string text)
		{
			foreach (var word in forbiddenwords)
			{
				text = text.Replace(word, new string('*', word.Length), StringComparison.InvariantCultureIgnoreCase);
			}

			return text;
		}
	}
}
