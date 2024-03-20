namespace GhostfolioSidekick.Parsers
{
	public interface ITransactionFileImporter
	{
		Task<bool> CanParseActivities(string filename);

		Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName);
	}
}
