using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public abstract class PdfBaseParser
	{
		public Task<bool> CanParseActivities(string filename)
		{
			try
			{
				var records = ParseRecords(filename);
				return Task.FromResult(records.Any());
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		public Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName)
		{
			var records = ParseRecords(filename);
			holdingsAndAccountsCollection.AddPartialActivity(accountName, records);

			return Task.CompletedTask;
		}

		protected abstract List<PartialActivity> ParseRecords(string filename);
	}
}