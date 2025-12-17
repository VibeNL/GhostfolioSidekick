using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public abstract class PdfBaseParser(
		IPdfToText parsePDfToWords) : IActivityFileImporter
	{
		public Task<bool> CanParse(string filename)
		{
			try
			{
				if (!filename.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
				{
					return Task.FromResult(false);
				}

				var words = parsePDfToWords.GetText(filename);
				return Task.FromResult(CanParseRecords(words));
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		public Task ParseActivities(string filename, IActivityManager activityManager, string accountName)
		{
			var records = ParseRecords(parsePDfToWords.GetText(filename));
			activityManager.AddPartialActivity(accountName, records);

			return Task.CompletedTask;
		}

		protected abstract bool CanParseRecords(string words);

		protected abstract IEnumerable<PartialActivity> ParseRecords(string v);
	}
}