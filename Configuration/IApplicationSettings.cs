namespace GhostfolioSidekick.Configuration
{
	public interface IApplicationSettings
	{
		string FileImporterPath { get; }

		string DatabasePath { get; }

		string GhostfolioUrl { get; }

		string GhostfolioAccessToken { get; }

		int TrottleTimeout { get; }

		ConfigurationInstance ConfigurationInstance { get; }

		bool AllowAdminCalls { get; set; }
	}
}