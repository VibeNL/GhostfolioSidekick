using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace GhostfolioSidekick
{
	public class CopyDatabaseTask(
		DatabaseContext dbContext) : IScheduledWork
	{
		private const string BackupFileName = "GhostfolioSidekick_backup.db";
		private const string BackupFolderName = "Backups";
		private const int MaxBackupCount = 5;

		public TaskPriority Priority => TaskPriority.BackupDatabase;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Copy & Backup Database";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Copying database...");

			try
			{
				var sourceFile = DatabaseContext.DbFileName;
				var destinationFile = BackupFileName;

				if (!File.Exists(sourceFile))
				{
					logger.LogWarning($"Source database file '{sourceFile}' not found. Skipping copy.");
					return;
				}

				// Close any open connections to ensure clean copy
				await dbContext.Database.CloseConnectionAsync();

				// Copy the database file, overwriting if it already exists (runs every hour)
				await using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
				await using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
				await sourceStream.CopyToAsync(destinationStream);

				logger.LogInformation($"Database copied successfully to '{destinationFile}'.");

				// Create compressed backup in subfolder (only once per day)
				if (ShouldCreateDailyBackup(logger))
				{
					await CreateCompressedBackup(sourceFile, logger);
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

		private static bool ShouldCreateDailyBackup(ILogger logger)
		{
			try
			{
				if (!Directory.Exists(BackupFolderName))
				{
					return true;
				}

				var todayDate = DateTime.UtcNow.ToString("yyyyMMdd");
				var todayBackupPattern = $"GhostfolioSidekick_backup_{todayDate}_*.db.gz";
				var existingTodayBackups = Directory.GetFiles(BackupFolderName, todayBackupPattern);

				return existingTodayBackups.Length == 0;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error checking for existing daily backup. Will attempt to create backup.");
				return true;
			}
		}

		private static async Task CreateCompressedBackup(string sourceFile, ILogger logger)
		{
			try
			{
				// Ensure backup folder exists
				if (!Directory.Exists(BackupFolderName))
				{
					Directory.CreateDirectory(BackupFolderName);
					logger.LogInformation($"Created backup folder '{BackupFolderName}'.");
				}

				// Create timestamped backup filename
				var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
				var backupFileName = $"GhostfolioSidekick_backup_{timestamp}.db.gz";
				var backupFilePath = Path.Combine(BackupFolderName, backupFileName);

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

		private static Task CleanupOldBackups(ILogger logger)
		{
			try
			{
				var backupFiles = Directory.GetFiles(BackupFolderName, "GhostfolioSidekick_backup_*.db.gz")
					.Select(f => new FileInfo(f))
					.OrderByDescending(f => f.CreationTimeUtc)
					.ToList();

				if (backupFiles.Count > MaxBackupCount)
				{
					var filesToDelete = backupFiles.Skip(MaxBackupCount).ToList();
					foreach (var file in filesToDelete)
					{
						file.Delete();
						logger.LogInformation($"Deleted old backup: '{file.Name}'.");
					}

					logger.LogInformation($"Cleaned up {filesToDelete.Count} old backup(s). Kept the last {MaxBackupCount}.");
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
