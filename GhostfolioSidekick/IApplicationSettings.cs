using GhostfolioSidekick.Configuration;

namespace GhostfolioSidekick
{
	public interface IApplicationSettings
	{
		string FileImporterPath { get; }

		string GhostfolioUrl { get; }

		string GhostfolioAccessToken { get; }

		ConfigurationInstance ConfigurationInstance { get; }
	}
}