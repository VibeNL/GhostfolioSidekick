namespace GhostfolioSidekick.Parsers
{
	public interface IFileImporter
	{
		Task<bool> CanParseActivities(string filename);

		Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName);
	}
}
