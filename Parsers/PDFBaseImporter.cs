using GhostfolioSidekick.Parsers.PDFParser;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace GhostfolioSidekick.Parsers
{
	public abstract class PDFBaseImporter<T> : IFileImporter
	{
		public Task<bool> CanParseActivities(string filename)
		{
			try
			{
				using PdfDocument document = PdfDocument.Open(filename);
				var tokens = new List<Token>();

				for (var i = 0; i < document.NumberOfPages; i++)
				{
					Page page = document.GetPage(i + 1);
					foreach (var word in page.GetWords())
					{
						tokens.Add(new Token(word.Text));
					}
				}

				var records = ParseTokens(tokens);
				return Task.FromResult(records.Any());
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		protected abstract IEnumerable<T> ParseTokens(List<Token> tokens);

		public Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName)
		{
			throw new NotImplementedException();
		}

	}
}