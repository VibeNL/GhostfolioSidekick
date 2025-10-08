using AwesomeAssertions;
using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.UnitTests.Activities
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "<Pending>")]
	public class FileImporterTaskTests : IDisposable
	{
		private readonly Mock<ILogger<FileImporterTask>> _mockLogger;
		private readonly Mock<IApplicationSettings> _mockSettings;
		private readonly IMemoryCache _memoryCache;
		private readonly List<IFileImporter> _importers;
		private readonly FileImporterTask _fileImporterTask;
		private readonly DbContextFactory _dbContextFactory;

		public FileImporterTaskTests()
		{
			_mockLogger = new Mock<ILogger<FileImporterTask>>();
			_mockSettings = new Mock<IApplicationSettings>();
			_memoryCache = new MemoryCache(new MemoryCacheOptions());
			_importers =
			[
				new Mock<IFileImporter>().Object
			];

			_mockSettings.Setup(x => x.FileImporterPath).Returns("test/path");

			// Create a single DbContextFactory instance that will be shared across the test
			_dbContextFactory = new DbContextFactory();

			_fileImporterTask = new FileImporterTask(
				_mockLogger.Object,
				_mockSettings.Object,
				_importers,
				_dbContextFactory,
				_memoryCache);
		}

		public void Dispose()
		{
			_dbContextFactory.Dispose();
			_memoryCache.Dispose();
		}

		[Fact]
		public void Priority_ShouldReturnFileImporter()
		{
			// Act
			var priority = _fileImporterTask.Priority;

			// Assert
			priority.Should().Be(TaskPriority.FileImporter);
		}

		[Fact]
		public async Task DoWork_ShouldParseFilesAndStoreActivities()
		{
			// Arrange
			var directories = new[] { "test/path/dir1" };
			Directory.CreateDirectory(directories[0]);

			// Use the shared database factory to set up test data
			using (var setupContext = _dbContextFactory.CreateDbContext())
			{
				setupContext.Accounts.Add(new Account { Name = "TestAccount" });
				await setupContext.SaveChangesAsync();
			}

			var knownHash = "knownHash";
			_memoryCache.Set(nameof(FileImporterTask), knownHash);

			// Act
			await _fileImporterTask.DoWork();

			// Assert
			_mockLogger.VerifyLog(logger => logger.LogDebug("{Name} Starting to do work", nameof(FileImporterTask)), Times.Once);
			_mockLogger.VerifyLog(logger => logger.LogDebug("{Name} Done", nameof(FileImporterTask)), Times.Once);
		}

		[Fact]
		public async Task StoreAll_ShouldDeduplicateAndStoreActivities()
		{
			// Arrange - Set up account in database first
			int accountId;
			using (var setupContext = _dbContextFactory.CreateDbContext())
			{
				var account = new Account { Name = "TestAccount" };
				setupContext.Accounts.Add(account);
				await setupContext.SaveChangesAsync();
				accountId = account.Id; // Store just the ID to avoid tracking issues
			}

			// Store existing activities in the database using the account ID
			using (var setupContext = _dbContextFactory.CreateDbContext())
			{
				// Load the account from this context to ensure proper tracking
				var trackedAccount = await setupContext.Accounts.FindAsync(accountId);
				
				var existingActivities = new List<Activity>
				{
					new BuyActivity { TransactionId = "T1", Account = trackedAccount! },
					new BuyActivity { TransactionId = "T2", Account = trackedAccount! }
				};
				
				setupContext.Activities.AddRange(existingActivities);
				await setupContext.SaveChangesAsync();
			}

			// Act & Assert - Use a fresh context for the StoreAll operation
			using (var testContext = _dbContextFactory.CreateDbContext())
			{
				// Load the account in this context to ensure proper tracking
				var accountForTest = await testContext.Accounts.FindAsync(accountId);
				
				// Create new activities using the properly tracked account
				var newActivities = new List<Activity>
				{
					new BuyActivity 
					{ 
						TransactionId = "T2", 
						Account = accountForTest!
					},
					new BuyActivity 
					{ 
						TransactionId = "T3", 
						Account = accountForTest!
					}
				};

				// Execute the method under test
				await FileImporterTask.StoreAll(testContext, newActivities);

				// Verify results in the same context to avoid additional complexity
				var finalActivities = await testContext.Activities
					.Include(x => x.Account)
					.ToListAsync();

				finalActivities.Count.Should().Be(2);
				
				// Verify that we have T2 and T3 transactions
				finalActivities.Should().Contain(a => a.TransactionId == "T2");
				finalActivities.Should().Contain(a => a.TransactionId == "T3");
				finalActivities.Should().NotContain(a => a.TransactionId == "T1");
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "<Pending>")]
	public class DbContextFactory : IDbContextFactory<DatabaseContext>, IDisposable
	{
		private readonly string _databasePath;
		private bool _disposed = false;

		public DbContextFactory()
		{
			// Create a unique temporary database file for this test instance
			_databasePath = Path.Combine(Path.GetTempPath(), $"test_database_{Guid.NewGuid()}.db");
		}

		public DatabaseContext CreateDbContext()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(DbContextFactory));
			}

			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite($"Data Source={_databasePath}")
				.EnableSensitiveDataLogging() // This will help debug tracking issues
				.Options;

			var context = new DatabaseContext(options);
			context.Database.EnsureCreated();
			return context;
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				// Clean up the temporary database file
				if (File.Exists(_databasePath))
				{
					try
					{
						File.Delete(_databasePath);
					}
					catch
					{
						// Ignore deletion errors during cleanup
					}
				}
				_disposed = true;
			}
		}
	}
}
