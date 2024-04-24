using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public abstract class PdfBaseParser
	{
		private readonly IPdfToWordsParser parsePDfToWords;

		protected PdfBaseParser(IPdfToWordsParser parsePDfToWords)
		{
			this.parsePDfToWords = parsePDfToWords;
		}

		public Task<bool> CanParseActivities(string filename)
		{
			try
			{
				var records = ParseRecords(parsePDfToWords.ParseTokens(filename));
				return Task.FromResult(records.Any());
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		public Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName)
		{
			var records = ParseRecords(parsePDfToWords.ParseTokens(filename));
			holdingsAndAccountsCollection.AddPartialActivity(accountName, records);

			return Task.CompletedTask;
		}

		protected abstract List<PartialActivity> ParseRecords(List<SingleWordToken> words);
	}
}