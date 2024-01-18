namespace GhostfolioSidekick.Parsers
{
	public interface IFileImporter
	{
		Task<bool> CanParseActivities(string filenames);

		Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName);
	}
}
