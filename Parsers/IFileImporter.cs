namespace GhostfolioSidekick.Parsers
{
	public interface IFileImporter
	{
		Task<bool> CanParseActivities(string filenames);

		Task ParseActivities(string filename, IHoldingsAndAccountsCollection holdingsAndAccountsCollection, string accountName);
	}
}
