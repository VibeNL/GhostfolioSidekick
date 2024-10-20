using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public abstract class PdfBaseParser : IActivityFileImporter
	{
		private readonly IPdfToWordsParser parsePDfToWords;

		protected PdfBaseParser(IPdfToWordsParser parsePDfToWords)
		{
			this.parsePDfToWords = parsePDfToWords;
		}

		public Task<bool> CanParse(string filename)
		{
			try
			{
				var words = parsePDfToWords.ParseTokens(filename);
				return Task.FromResult(CanParseRecords(words));
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		public Task ParseActivities(string filename, IActivityManager activityManager, string accountName)
		{
			var records = ParseRecords(parsePDfToWords.ParseTokens(filename));
			activityManager.AddPartialActivity(accountName, records);

			return Task.CompletedTask;
		}

		protected abstract bool CanParseRecords(List<SingleWordToken> words);

		protected abstract List<PartialActivity> ParseRecords(List<SingleWordToken> words);


		protected static bool IsCheckWords(string check, List<SingleWordToken> words, int i)
		{
			var splitted = check.Split(" ");
			for (int j = 0; j < splitted.Length; j++)
			{
				var expected = splitted[j];
				var actual = words[i + j].Text;
				if (expected != actual)
				{
					return false;
				}
			}

			return true;
		}
	}
}