using GhostfolioSidekick.Configuration;
using System.IO.Compression;

namespace GhostfolioSidekick.UnitTests
{
	public class CopyDatabaseTaskTests : IDisposable
	{
		private readonly Mock<IApplicationSettings> applicationSettingsMock;
		private readonly Mock<ILogger> loggerMock;
		private readonly CopyDatabaseTask copyDatabaseTask;
		private readonly string tempDirectory;
		private readonly string testDbPath;
		private readonly string backupDbPath;
		private readonly string backupFolderPath;

		public CopyDatabaseTaskTests()
		{
			applicationSettingsMock = new Mock<IApplicationSettings>();
			loggerMock = new Mock<ILogger>();

			// Create a temporary directory for testing
			tempDirectory = Path.Combine(Path.GetTempPath(), $"CopyDatabaseTaskTests_{Guid.NewGuid()}");
			Directory.CreateDirectory(tempDirectory);

			testDbPath = Path.Combine(tempDirectory, "test.db");
			backupDbPath = Path.Combine(tempDirectory, "GhostfolioSidekick_backup.db");
			backupFolderPath = Path.Combine(tempDirectory, "backups");

			applicationSettingsMock.Setup(x => x.DatabaseFilePath).Returns(testDbPath);
			applicationSettingsMock.Setup(x => x.BackupDatabaseFilePath).Returns(backupDbPath);
			applicationSettingsMock.Setup(x => x.BackupFolderName).Returns(backupFolderPath);
			applicationSettingsMock.Setup(x => x.MaxBackupCount).Returns(5);

			copyDatabaseTask = new CopyDatabaseTask(applicationSettingsMock.Object);
		}

		public void Dispose()
		{
			// Clean up temporary directory
			if (Directory.Exists(tempDirectory))
			{
				try
				{
					Directory.Delete(tempDirectory, true);
				}
				catch
				{
					// Best effort cleanup - sometimes files are locked briefly
				}
			}
		}

		[Fact]
		public async Task DoWork_WhenSourceDatabaseNotFound_ShouldLogWarningAndReturn()
		{
			// Arrange - don't create the test database file

			// Act
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_WithValidSourceDatabase_ShouldCreateBackupFile()
		{
			// Arrange
			await CreateTestDatabase(testDbPath);

			// Act
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert
			Assert.True(File.Exists(backupDbPath), "Backup file should be created");

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database copied successfully")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_WhenBackupFolderNotExists_ShouldCreateCompressedBackup()
		{
			// Arrange
			await CreateTestDatabase(testDbPath);

			// Act
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert - should create backup folder and compressed backup
			Assert.True(Directory.Exists(backupFolderPath), "Backup folder should be created");
			
			var compressedBackups = Directory.GetFiles(backupFolderPath, "GhostfolioSidekick_backup_*.db.gz");
			Assert.NotEmpty(compressedBackups);

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created backup folder")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Compressed backup created")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_WhenDailyBackupAlreadyExists_ShouldSkipCompressedBackup()
		{
			// Arrange
			await CreateTestDatabase(testDbPath);
			Directory.CreateDirectory(backupFolderPath);

			// Create an existing backup for today
			var todayDate = DateTime.UtcNow.ToString("yyyyMMdd");
			var existingBackupName = $"GhostfolioSidekick_backup_{todayDate}_120000.db.gz";
			var existingBackupPath = Path.Combine(backupFolderPath, existingBackupName);
			await File.WriteAllTextAsync(existingBackupPath, "dummy content", TestContext.Current.CancellationToken);

			// Act
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert - should not create another compressed backup
			var compressedBackups = Directory.GetFiles(backupFolderPath, "GhostfolioSidekick_backup_*.db.gz");
			Assert.Single(compressedBackups); // Only the one we created

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already exists for today")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_WithMoreThanMaxBackups_ShouldCleanupOldBackups()
		{
			// Arrange
			await CreateTestDatabase(testDbPath);
			Directory.CreateDirectory(backupFolderPath);

			// Create 6 old backups (more than MaxBackupCount of 5)
			for (int i = 0; i < 6; i++)
			{
				var date = DateTime.UtcNow.AddDays(-i - 1); // All different days
				var backupName = $"GhostfolioSidekick_backup_{date:yyyyMMdd}_120000.db.gz";
				var backupPath = Path.Combine(backupFolderPath, backupName);
				await File.WriteAllTextAsync(backupPath, "dummy content", TestContext.Current.CancellationToken);
				
				// Set creation time to ensure proper ordering
				File.SetCreationTimeUtc(backupPath, date);
			}

			// Act
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert - should keep only 5 most recent backups (the 6 old ones + 1 new one = 7 total, keep 5)
			var remainingBackups = Directory.GetFiles(backupFolderPath, "GhostfolioSidekick_backup_*.db.gz");
			Assert.Equal(5, remainingBackups.Length);

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleaned up")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_CompressedBackup_ShouldBeValidGzip()
		{
			// Arrange
			await CreateTestDatabase(testDbPath);

			// Act
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert - verify compressed file is valid
			var compressedBackup = Directory.GetFiles(backupFolderPath, "GhostfolioSidekick_backup_*.db.gz").First();
			
			// Try to decompress it to verify it's valid
			using (var fileStream = File.OpenRead(compressedBackup))
			using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
			using (var memoryStream = new MemoryStream())
			{
				await gzipStream.CopyToAsync(memoryStream, TestContext.Current.CancellationToken);
				Assert.True(memoryStream.Length > 0, "Decompressed content should not be empty");
			}
		}

		[Fact]
		public void TaskProperties_ShouldHaveCorrectValues()
		{
			// Assert
			Assert.Equal(TaskPriority.BackupDatabase, copyDatabaseTask.Priority);
			Assert.Equal(Frequencies.Hourly, copyDatabaseTask.ExecutionFrequency);
			Assert.False(copyDatabaseTask.ExceptionsAreFatal);
			Assert.Equal("Copy & Backup Database", copyDatabaseTask.Name);
		}

		[Fact]
		public async Task DoWork_WhenCompressedBackupFails_ShouldLogErrorButContinue()
		{
			// Arrange
			await CreateTestDatabase(testDbPath);
			
			// Set an invalid backup folder path to cause compressed backup to fail
			applicationSettingsMock.Setup(x => x.BackupFolderName).Returns("C:\0Invalid:\0Path");

			// Act - should not throw, main backup should still succeed
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert - main backup should succeed
			Assert.True(File.Exists(backupDbPath), "Main backup file should be created even if compressed backup fails");

			// Compressed backup failure should be logged but not throw
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create compressed backup")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_MultipleBackupsOnDifferentDays_ShouldCreateMultipleFiles()
		{
			// Arrange
			await CreateTestDatabase(testDbPath);
			Directory.CreateDirectory(backupFolderPath);

			// Create backups for different days (simulating multiple runs)
			var yesterday = DateTime.UtcNow.AddDays(-1);
			var yesterdayBackupName = $"GhostfolioSidekick_backup_{yesterday:yyyyMMdd}_120000.db.gz";
			var yesterdayBackupPath = Path.Combine(backupFolderPath, yesterdayBackupName);
			await File.WriteAllTextAsync(yesterdayBackupPath, "dummy content", TestContext.Current.CancellationToken);
			File.SetCreationTimeUtc(yesterdayBackupPath, yesterday);

			// Act - create today's backup
			await copyDatabaseTask.DoWork(loggerMock.Object);

			// Assert - should have both backups
			var allBackups = Directory.GetFiles(backupFolderPath, "GhostfolioSidekick_backup_*.db.gz");
			Assert.Equal(2, allBackups.Length);
		}

		private static async Task CreateTestDatabase(string path)
		{
			// Create a minimal SQLite database
			var connectionString = $"Data Source={path}";
			using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
			await connection.OpenAsync();

			using var command = connection.CreateCommand();
			command.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT);";
			await command.ExecuteNonQueryAsync();

			command.CommandText = "INSERT INTO TestTable (Name) VALUES ('Test');";
			await command.ExecuteNonQueryAsync();

			await connection.CloseAsync();
		}
	}
}
