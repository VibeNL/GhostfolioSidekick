namespace GhostfolioSidekick.Configuration
{
	public interface IApplicationSettings
	{
		string FileImporterPath { get; }

		string DatabasePath { get; }

		string DatabaseFilePath { get; }

		string GhostfolioUrl { get; }

		string GhostfolioAccessToken { get; }

		int TrottleTimeout { get; }

		/// <summary>
		/// Database query timeout in seconds for complex queries. Default is 120 seconds.
		/// </summary>
		int DatabaseQueryTimeoutSeconds { get; }

		/// <summary>
		/// Whether to enable detailed database performance logging. Default is false.
		/// </summary>
		bool EnableDatabasePerformanceLogging { get; }

		/// <summary>
		/// Folder name for database backups. Default is "Backups".
		/// </summary>
		string BackupFolderName { get; }

		/// <summary>
		/// Maximum number of compressed backups to keep. Default is 5.
		/// </summary>
		int MaxBackupCount { get; }

		ConfigurationInstance ConfigurationInstance { get; }

		bool AllowAdminCalls { get; set; }
	}
}