namespace GhostfolioSidekick.FileImporter
{
	public interface IFileImporter
	{
		Task<bool> CanParseActivities(IEnumerable<string> filenames);

		Task<Model.Account> ConvertActivitiesForAccount(string accountName, IEnumerable<string> filenames);
	}
}
