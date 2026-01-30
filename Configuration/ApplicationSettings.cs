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
		private const string DATABASE_QUERY_TIMEOUT = "DATABASE_QUERY_TIMEOUT_SECONDS";
		private const string ENABLE_DATABASE_PERFORMANCE_LOGGING = "ENABLE_DATABASE_PERFORMANCE_LOGGING";
		private const string BACKUP_FOLDER_NAME = "BACKUP_FOLDER_NAME";
		private const string MAX_BACKUP_COUNT = "MAX_BACKUP_COUNT";
		private const string DEFAULT_DB_NAME = "ghostfolio.db";

		public ApplicationSettings(ILogger<ApplicationSettings> logger)
		{
			try
			{
				configuration = ConfigurationInstance.Parse(File.ReadAllText(Environment.GetEnvironmentVariable(CONFIGURATIONFILE)!))!;
				ArgumentNullException.ThrowIfNull(configuration);
			}
			catch (Exception ex)
			{
				logger.LogCritical(ex, "No (valid) configuration file found at {Configfile}. Using default configuration. Error was {Message}", Environment.GetEnvironmentVariable(CONFIGURATIONFILE), ex.Message);
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
					dbPath = Path.Combine(dbPath, DEFAULT_DB_NAME);
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

		/// <summary>
		/// Database query timeout in seconds for complex queries. Default is 120 seconds.
		/// </summary>
		public int DatabaseQueryTimeoutSeconds => GetDatabaseQueryTimeout();

		/// <summary>
		/// Whether to enable detailed database performance logging. Default is false.
		/// </summary>
		public bool EnableDatabasePerformanceLogging => GetDatabasePerformanceLogging();

		/// <summary>
		/// Folder name for database backups. Default is "GHOSTFOLIOSIDEKICKBACKUPS".
		/// </summary>
		public string BackupFolderName => GetBackupFolderName();

		/// <summary>
		/// Maximum number of compressed backups to keep. Default is 5.
		/// </summary>
		public int MaxBackupCount => GetMaxBackupCount();

		private static int GetTimeout()
		{
			if (int.TryParse(Environment.GetEnvironmentVariable(TROTTLETIMEOUT), out int timeoutInSeconds))
			{
				return timeoutInSeconds;
			}

			return 0;
		}

		private static int GetDatabaseQueryTimeout()
		{
			if (int.TryParse(Environment.GetEnvironmentVariable(DATABASE_QUERY_TIMEOUT), out int timeoutInSeconds))
			{
				return timeoutInSeconds;
			}

			return 120; // Default 2 minutes for complex portfolio queries
		}

		private static bool GetDatabasePerformanceLogging()
		{
			if (bool.TryParse(Environment.GetEnvironmentVariable(ENABLE_DATABASE_PERFORMANCE_LOGGING), out bool enabled))
			{
				return enabled;
			}

			return false; // Default disabled
		}

		private string GetBackupFolderName()
		{
			var baseLocation = Path.GetDirectoryName(DatabaseFilePath);
			var folderName = Environment.GetEnvironmentVariable(BACKUP_FOLDER_NAME);
			var backupFolderName = string.IsNullOrEmpty(folderName) ? "GHOSTFOLIOSIDEKICKBACKUPS" : folderName;
			return Path.Combine(baseLocation!, backupFolderName);
		}

		private static int GetMaxBackupCount()
		{
			if (int.TryParse(Environment.GetEnvironmentVariable(MAX_BACKUP_COUNT), out int count) && count > 0)
			{
				return count;
			}

			return 5; // Default keep last 5 backups
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
