namespace GhostfolioSidekick
{
	public class ConfigurationSettings : IConfigurationSettings
	{
		public string? FileImporterPath => Environment.GetEnvironmentVariable("FileImporterPath");

		public string GhostfolioAccessToken => Environment.GetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN");

		public string GhostfolioUrl
		{
			get
			{
				var url = ghostfolioUrl;
				if (url != null && url.EndsWith('/'))
				{
					url = url[..^1];
				}

				return url;
			}
		}

		private readonly string? ghostfolioUrl = Environment.GetEnvironmentVariable("GHOSTFOLIO_URL");
	}
}
