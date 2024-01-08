using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.FileImporter
{
	public interface IFileImporter
	{
		Task<bool> CanParseActivities(string fileName);

		Task<IEnumerable<Activity>> ConvertToActivities(string fileName, Balance accountBalance);
	}
}
