using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace GhostfolioSidekick
{
	public class CopyDatabaseTask(
		//IDbContextFactory<DatabaseContext> dbContextFactory,
		IApplicationSettings settings) : IScheduledWork
	{
		private const string BackupFileName = "GhostfolioSidekick_backup.db";

		public TaskPriority Priority => TaskPriority.BackupDatabase;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Copy & Backup Database";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Copying database...");

			try
			{
				var sourceFile = settings.DatabaseFilePath;
				var destinationFile = Path.Combine(Path.GetDirectoryName(sourceFile)!, BackupFileName);

				if (!File.Exists(sourceFile))
				{
					logger.LogWarning($"Source database file '{sourceFile}' not found. Skipping copy.");
					return;
				}

				// Use SQLite's BackupDatabase API to safely backup while database is in use
				await BackupDatabaseUsingSqliteApi(sourceFile, destinationFile, logger);

				logger.LogInformation($"Database copied successfully to '{destinationFile}'.");

				// Create compressed backup in subfolder (only once per day)
				if (ShouldCreateDailyBackup(logger))
				{
					await CreateCompressedBackup(destinationFile, logger);
				}
				else
				{
					logger.LogInformation("Compressed backup already exists for today. Skipping.");
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to copy database.");
				throw;
			}
		}

		private async Task BackupDatabaseUsingSqliteApi(string sourceFile, string destinationFile, ILogger logger)
		{
			var sourceConnectionString = $"Data Source={sourceFile}";
			var destinationConnectionString = $"Data Source={destinationFile}";

			await using var sourceConnection = new SqliteConnection(sourceConnectionString);
			await using var destinationConnection = new SqliteConnection(destinationConnectionString);

			await sourceConnection.OpenAsync();
			await destinationConnection.OpenAsync();

			// Use SQLite's backup API - this works even when the database is in use
			sourceConnection.BackupDatabase(destinationConnection);
		}

		private bool ShouldCreateDailyBackup(ILogger logger)
		{
			try
			{
				if (!Directory.Exists(settings.BackupFolderName))
				{
					return true;
				}

				var todayDate = DateTime.UtcNow.ToString("yyyyMMdd");
				var todayBackupPattern = $"GhostfolioSidekick_backup_{todayDate}_*.db.gz";
				var existingTodayBackups = Directory.GetFiles(settings.BackupFolderName, todayBackupPattern);

				return existingTodayBackups.Length == 0;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error checking for existing daily backup. Will attempt to create backup.");
				return true;
			}
		}


		private async Task CreateCompressedBackup(string sourceFile, ILogger logger)
		{
			try
			{
				// Ensure backup folder exists
				if (!Directory.Exists(settings.BackupFolderName))
				{
					Directory.CreateDirectory(settings.BackupFolderName);
					logger.LogInformation($"Created backup folder '{settings.BackupFolderName}'.");
				}

				// Create timestamped backup filename
				var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
				var backupFileName = $"GhostfolioSidekick_backup_{timestamp}.db.gz";
				var backupFilePath = Path.Combine(settings.BackupFolderName, backupFileName);

				// Compress and save backup using async streams
				await using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
				await using var destinationStream = new FileStream(backupFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
				await using var compressionStream = new GZipStream(destinationStream, CompressionLevel.Optimal);
				await sourceStream.CopyToAsync(compressionStream);

				logger.LogInformation($"Compressed backup created: '{backupFilePath}'.");

				// Clean up old backups, keeping only the last 5
				await CleanupOldBackups(logger);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to create compressed backup.");
				// Don't throw - the main backup already succeeded
			}
		}

		private Task CleanupOldBackups(ILogger logger)
		{
			try
			{
				var backupFiles = Directory.GetFiles(settings.BackupFolderName, "GhostfolioSidekick_backup_*.db.gz")
					.Select(f => new FileInfo(f))
					.OrderByDescending(f => f.CreationTimeUtc)
					.ToList();

				if (backupFiles.Count > settings.MaxBackupCount)
				{
					var filesToDelete = backupFiles.Skip(settings.MaxBackupCount).ToList();
					foreach (var file in filesToDelete)
					{
						file.Delete();
						logger.LogInformation($"Deleted old backup: '{file.Name}'.");
					}

					logger.LogInformation($"Cleaned up {filesToDelete.Count} old backup(s). Kept the last {settings.MaxBackupCount}.");
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to cleanup old backups.");
				// Don't throw - this is not critical
			}

			return Task.CompletedTask;
		}
	}
}
