namespace GhostfolioSidekick.Configuration
{
	public class ApplicationSettings : IApplicationSettings
	{
		private const string URL = "GHOSTFOLIO_URL";
		private const string ACCESSTOKEN = "GHOSTFOLIO_ACCESTOKEN";
		private const string PATHFILES = "FILEIMPORTER_PATH";
		private const string CONFIGURATIONFILE = "CONFIGURATIONFILE_PATH";

		public ApplicationSettings()
		{
			configuration = ConfigurationInstance.Parse(File.ReadAllText(Environment.GetEnvironmentVariable(CONFIGURATIONFILE)!));
		}

		public string FileImporterPath => Environment.GetEnvironmentVariable(PATHFILES)!;

		public string GhostfolioAccessToken => Environment.GetEnvironmentVariable(ACCESSTOKEN)!;

		public string GhostfolioUrl
		{
			get
			{
				string url = ghostfolioUrl;
				if (url.EndsWith('/'))
				{
					url = url[..^1];
				}

				return url;
			}
		}

		public ConfigurationInstance ConfigurationInstance
		{
			get
			{
				return configuration ?? new ConfigurationInstance();
			}
		}

		public bool AllowAdminCalls { get; set; } = true;

		private readonly string ghostfolioUrl = Environment.GetEnvironmentVariable(URL)!;
		private readonly ConfigurationInstance? configuration;
	}
}
