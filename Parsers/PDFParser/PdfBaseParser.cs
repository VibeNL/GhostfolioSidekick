using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public abstract class PdfBaseParser(IPdfToWordsParser parsePDfToWords) : IActivityFileImporter
	{
		/// <summary>
		/// Gets the footer height threshold for this parser.
		/// Override this property in derived classes to customize footer filtering.
		/// </summary>
		protected virtual int FooterHeightThreshold => 50;

		/// <summary>
		/// Determines whether this parser should ignore footer content by default.
		/// Override this property in derived classes to enable footer filtering.
		/// </summary>
		protected virtual bool IgnoreFooter => false;

		public Task<bool> CanParse(string filename)
		{
			try
			{
				if (!filename.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
				{
					return Task.FromResult(false);
				}

				var words = GetWords(filename);
				return Task.FromResult(CanParseRecords(filename, words));
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		public Task ParseActivities(string filename, IActivityManager activityManager, string accountName)
		{
			var words = GetWords(filename);
			var records = ParseRecords(filename, words);
			activityManager.AddPartialActivity(accountName, records);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets words from the PDF, optionally filtering out footer content.
		/// </summary>
		/// <param name="filename">Path to the PDF file</param>
		/// <returns>List of word tokens</returns>
		protected virtual List<SingleWordToken> GetWords(string filename)
		{
			return IgnoreFooter 
				? parsePDfToWords.ParseTokensIgnoringFooter(filename, FooterHeightThreshold)
				: parsePDfToWords.ParseTokens(filename);
		}

		protected abstract bool CanParseRecords(string filename, List<SingleWordToken> words);

		protected abstract List<PartialActivity> ParseRecords(string filename, List<SingleWordToken> words);


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