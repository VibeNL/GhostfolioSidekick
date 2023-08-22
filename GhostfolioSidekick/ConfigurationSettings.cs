namespace GhostfolioSidekick
{
	public class ConfigurationSettings : IConfigurationSettings
	{
		public string? FileImporterPath => Environment.GetEnvironmentVariable("FileImporterPath");
	}
}
