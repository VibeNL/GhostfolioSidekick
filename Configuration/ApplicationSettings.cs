using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Configuration
{
	public class ApplicationSettings : IApplicationSettings
	{
		private const string URL = "GHOSTFOLIO_URL";
		private const string ACCESSTOKEN = "GHOSTFOLIO_ACCESTOKEN";
		private const string PATHFILES = "FILEIMPORTER_PATH";
		private const string DATABASEPATH = "DATABASE_PATH";
		private const string CONFIGURATIONFILE = "CONFIGURATIONFILE_PATH";
		private const string TROTTLETIMEOUT = "TROTTLE_WAITINSECONDS";

		public ApplicationSettings(ILogger<ApplicationSettings> logger)
		{
			try
			{
				configuration = ConfigurationInstance.Parse(File.ReadAllText(Environment.GetEnvironmentVariable(CONFIGURATIONFILE)!))!;
				ArgumentNullException.ThrowIfNull(configuration);
			}
			catch (Exception ex)
			{
				logger.LogCritical("No (valid) configuration file found at {Configfile}. Using default configuration. Error was {Message}", Environment.GetEnvironmentVariable(CONFIGURATIONFILE), ex.Message);

				// Wait for 1 minute to make the point!
				Thread.Sleep(60000);

				configuration = new ConfigurationInstance();
			}
		}

		public string FileImporterPath => Environment.GetEnvironmentVariable(PATHFILES)!;

		public string DatabasePath => Environment.GetEnvironmentVariable(DATABASEPATH)!;

		public string DatabaseFilePath
		{
			get
			{
				var dbPath = DatabasePath ?? FileImporterPath;
				if (string.IsNullOrEmpty(dbPath))
				{
					throw new InvalidOperationException("Database path is not configured.");
				}

				if (!dbPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
				{
					dbPath = Path.Combine(dbPath, "ghostfolio.db");
				}

				return dbPath;
			}
		}

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
		public int TrottleTimeout => GetTimeout();

		private static int GetTimeout()
		{
			if (int.TryParse(Environment.GetEnvironmentVariable(TROTTLETIMEOUT), out int timeoutInSeconds))
			{
				return timeoutInSeconds;
			}

			return 0;
		}

		public ConfigurationInstance ConfigurationInstance
		{
			get
			{
				return configuration;
			}
		}

		public bool AllowAdminCalls { get; set; } = true;

		private readonly string ghostfolioUrl = Environment.GetEnvironmentVariable(URL)!;
		private readonly ConfigurationInstance configuration;
	}
}
