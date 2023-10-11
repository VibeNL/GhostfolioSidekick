namespace GhostfolioSidekick
{
	public interface IConfigurationSettings
	{
		string FileImporterPath { get; }

		string GhostfolioUrl { get; }

		string GhostfolioAccessToken { get; }
	}
}