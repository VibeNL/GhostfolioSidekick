using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.UnitTests.Activities
{
	public class FileImporterTaskTests
	{
		private readonly Mock<ILogger<FileImporterTask>> _mockLogger;
		private readonly Mock<IApplicationSettings> _mockSettings;
		private readonly IMemoryCache _memoryCache;
		private readonly List<IFileImporter> _importers;
		private readonly FileImporterTask _fileImporterTask;

		public FileImporterTaskTests()
		{
			_mockLogger = new Mock<ILogger<FileImporterTask>>();
			_mockSettings = new Mock<IApplicationSettings>();
			_memoryCache = new MemoryCache(new MemoryCacheOptions());
			_importers = new List<IFileImporter>
			{
				new Mock<IFileImporter>().Object
			};

			_mockSettings.Setup(x => x.FileImporterPath).Returns("test/path");

			_fileImporterTask = new FileImporterTask(
				_mockLogger.Object,
				_mockSettings.Object,
				_importers,
				new DbContextFactory(),
				_memoryCache);
		}

		[Fact]
		public void Priority_ShouldReturnFileImporter()
		{
			// Act
			var priority = _fileImporterTask.Priority;

			// Assert
			priority.ShouldBe(TaskPriority.FileImporter);
		}

		[Fact(Skip = "todo implement real db")]
		public async Task DoWork_ShouldParseFilesAndStoreActivities()
		{
			// Arrange
			var directories = new[] { "test/path/dir1" };
			Directory.CreateDirectory(directories[0]);

			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseInMemoryDatabase(databaseName: "TestDatabase")
				.Options;

			using var context = new DatabaseContext(options);
			context.Accounts.Add(new Account { Name = "TestAccount" });
			await context.SaveChangesAsync();

			var knownHash = "knownHash";
			_memoryCache.Set(nameof(FileImporterTask), knownHash);

			// Act
			await _fileImporterTask.DoWork();

			// Assert
			_mockLogger.VerifyLog(logger => logger.LogDebug("{Name} Starting to do work", nameof(FileImporterTask)), Times.Once);
			_mockLogger.VerifyLog(logger => logger.LogDebug("{Name} Done", nameof(FileImporterTask)), Times.Once);
		}

		[Fact(Skip = "todo implement real db")]
		public async Task StoreAll_ShouldDeduplicateAndStoreActivities()
		{
			// Arrange
			var existingActivities = new List<Activity>
			{
				new BuySellActivity { TransactionId = "T1" },
				new BuySellActivity { TransactionId = "T2" }
			};

			var newActivities = new List<Activity>
			{
				new BuySellActivity { TransactionId = "T2" },
				new BuySellActivity { TransactionId = "T3" }
			};

			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseInMemoryDatabase(databaseName: "TestDatabase")
				.Options;

			using var context = new DatabaseContext(options);
			context.Activities.AddRange(existingActivities);
			await context.SaveChangesAsync();

			// Act
			await _fileImporterTask.StoreAll(context, newActivities);

			// Assert
			(await context.Activities.CountAsync()).ShouldBe(2);
			context.Activities.ShouldContain(newActivities[1]);
			context.Activities.ShouldContain(existingActivities[1]);
			context.Activities.ShouldNotContain(existingActivities[0]);
		}
	}

	public class DbContextFactory : IDbContextFactory<DatabaseContext>
	{
		public DatabaseContext CreateDbContext()
		{
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseInMemoryDatabase(databaseName: "TestDatabase")
				.Options;
			return new DatabaseContext(options);
		}
	}
}
