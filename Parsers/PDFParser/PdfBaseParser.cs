using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public abstract class PdfBaseParser(IPdfToWordsParser parsePDfToWords) : IActivityFileImporter
	{
		public Task<bool> CanParse(string filename)
		{
			try
			{
				if (!filename.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
				{
					return Task.FromResult(false);
				}

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


		protected static bool IsCheckWords(string check, List<SingleWordToken> words, int i, bool caseInsentitive = false)
		{
			var splitted = check.Split(" ");
			for (int j = 0; j < splitted.Length; j++)
			{
				var expected = splitted[j];
				var actual = words[i + j].Text;
				if (!string.Equals(expected, actual, caseInsentitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
				{
					return false;
				}
			}

			return true;
		}

		protected static bool ContainsSequence (string[] sequence, List<SingleWordToken> words, bool caseInsentitive = false)
		{
			for (int i = 0; i <= words.Count - sequence.Length; i++)
			{
				bool match = true;
				for (int j = 0; j < sequence.Length; j++)
				{
					var expected = sequence[j];
					var actual = words[i + j].Text;
					if (!string.Equals(expected, actual, caseInsentitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
					{
						match = false;
						break;
					}
				}

				if (match)
				{
					return true;
				}
			}
			return false;
		}
	}
}