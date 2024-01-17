using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public interface IFileImporter
	{
		Task<bool> CanParseActivities(string filenames);

		Task ParseActivities(string filename, HoldingsAndAccountsCollection holdingsAndAccountsCollection, string accountName);
	}
}
