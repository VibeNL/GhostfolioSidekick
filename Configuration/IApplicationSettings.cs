namespace GhostfolioSidekick.Configuration
{
	public interface IApplicationSettings
	{
		string FileImporterPath { get; }

		string DatabasePath { get; }

		string DatabaseFilePath { get; }

		string BackupDatabaseFilePath { get; }

		string GhostfolioUrl { get; }

		string GhostfolioAccessToken { get; }

		int ThrottleTimeout { get; }

		/// <summary>
		/// Database query timeout in seconds for complex queries. Default is 120 seconds.
		/// </summary>
		int DatabaseQueryTimeoutSeconds { get; }

		/// <summary>
		/// Whether to enable detailed database performance logging. Default is false.
		/// </summary>
		bool EnableDatabasePerformanceLogging { get; }

		/// <summary>
		/// Folder name for database backups. Default is "GHOSTFOLIOSIDEKICKBACKUPS".
		/// </summary>
		string BackupFolderName { get; }

		/// <summary>
		/// Maximum number of compressed backups to keep. Default is 5.
		/// </summary>
		int MaxBackupCount { get; }

		/// <summary>
		/// HTTP cache expiry in hours for CoinGecko API calls. Default is 24.
		/// </summary>
		int CoinGeckoCacheExpiryHours { get; }

		/// <summary>
		/// HTTP cache expiry in hours for Yahoo Finance API calls. Default is 24.
		/// </summary>
		int YahooCacheExpiryHours { get; }

		/// <summary>
		/// HTTP cache expiry in hours for DividendMax API calls. Default is 168.
		/// </summary>
		int DividendMaxCacheExpiryHours { get; }

		/// <summary>
		/// HTTP cache expiry in hours for Ghostfolio API calls. Default is 168.
		/// </summary>
		int GhostfolioCacheExpiryHours { get; }

		ConfigurationInstance ConfigurationInstance { get; }

		bool AllowAdminCalls { get; set; }
	}
}