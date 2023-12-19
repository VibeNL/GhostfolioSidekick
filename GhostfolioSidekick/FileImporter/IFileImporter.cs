namespace GhostfolioSidekick.FileImporter
{
	public interface IFileImporter
	{
		Task<bool> CanParseActivities(string fileName);

		Task<IEnumerable<Model.Activity>> ConvertToActivities(string fileName);
	}
}
