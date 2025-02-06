namespace GhostfolioSidekick.Parsers
{
	public interface IActivityFileImporter : IFileImporter
	{
		Task ParseActivities(string filename, IActivityManager activityManager, string accountName);
	}
}
