using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace GhostfolioSidekick.Parsers
{
	public class PDFBaseImporter : IFileImporter
	{
		public Task<bool> CanParseActivities(string filename)
		{
			try
			{
				using PdfDocument document = PdfDocument.Open(filename);
				var text = new StringBuilder();

				for (var i = 0; i < document.NumberOfPages; i++)
				{
					Page page = document.GetPage(i + 1);
					foreach (var word in page.GetWords())
					{
						text.AppendLine(word.Text);
					}
				}
			}
			catch
			{
				return Task.FromResult(false);
			}

			return Task.FromResult(true);
		}

		public Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName)
		{
			throw new NotImplementedException();
		}
	}
}