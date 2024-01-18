namespace GhostfolioSidekick.Configuration
{
	public interface IApplicationSettings
	{
		string FileImporterPath { get; }

		string GhostfolioUrl { get; }

		string GhostfolioAccessToken { get; }

		ConfigurationInstance ConfigurationInstance { get; }
	}
}