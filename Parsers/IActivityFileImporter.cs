namespace GhostfolioSidekick.Parsers
{
	public interface IActivityFileImporter : IFileImporter
	{
		Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName);
	}
}
