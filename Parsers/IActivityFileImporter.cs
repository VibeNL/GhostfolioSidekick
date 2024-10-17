namespace GhostfolioSidekick.Parsers
{
	public interface IActivityFileImporter : IFileImporter
	{
		Task ParseActivities(string filename, IActivityManager holdingsAndAccountsCollection, string accountName);
	}
}
