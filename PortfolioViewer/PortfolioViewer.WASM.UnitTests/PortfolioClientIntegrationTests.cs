using System.Text.Json;
using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
	/// <summary>
	/// Integration tests for PortfolioClient that test more complex scenarios 
	/// with real database connections and mocked gRPC services
	/// </summary>
	public class PortfolioClientIntegrationTests : IDisposable
	{
		private readonly SqliteConnection _connection;
		private readonly DbContextOptions<DatabaseContext> _contextOptions;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly Mock<ISyncTrackingService> _mockSyncTrackingService;
		private readonly Mock<ILogger<PortfolioClient>> _mockLogger;
		private readonly Mock<SqlitePersistence> _mockSqlitePersistence;
		private bool _disposed;

		public PortfolioClientIntegrationTests()
		{
			// Create and open a connection. This creates the SQLite in-memory database, which will persist until the connection is closed
			// at the end of the test (see Dispose below).
			_connection = new SqliteConnection("Filename=:memory:");
			_connection.Open();

			// These options will be used by the context instances in this test suite, including the connection opened above.
			_contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(_connection)
				.Options;

			// Create the schema and seed some data
			using var context = new DatabaseContext(_contextOptions);
			if (context.Database.EnsureCreated())
			{
				// Seed some test data if needed
			}

			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_mockSyncTrackingService = new Mock<ISyncTrackingService>();
			_mockLogger = new Mock<ILogger<PortfolioClient>>();
			_mockSqlitePersistence = new Mock<SqlitePersistence>(Mock.Of<Microsoft.JSInterop.IJSRuntime>());
		}

		[Fact]
		public void PortfolioClient_ShouldInitializeCorrectly()
		{
			// Arrange
			using var context = new DatabaseContext(_contextOptions);
			var dbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			dbContextFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(context);

			using var httpClient = new HttpClient { BaseAddress = new Uri("https://test.example.com") };

			// Act & Assert
			using var portfolioClient = new PortfolioClient(
				httpClient,
				_mockSqlitePersistence.Object,
				_mockCurrencyExchange.Object,
				dbContextFactory.Object,
				_mockSyncTrackingService.Object,
				_mockLogger.Object);

			Assert.NotNull(portfolioClient);
		}

		[Fact]
		public void PortfolioClient_ShouldCreateWithoutBaseAddress()
		{
			// Arrange
			using var context = new DatabaseContext(_contextOptions);
			var dbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			dbContextFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(context);

			using var httpClient = new HttpClient(); // No BaseAddress

			// Act - The PortfolioClient doesn't throw in constructor, it throws when trying to use gRPC
			using var portfolioClient = new PortfolioClient(
				httpClient,
				_mockSqlitePersistence.Object,
				_mockCurrencyExchange.Object,
				dbContextFactory.Object,
				_mockSyncTrackingService.Object,
				_mockLogger.Object);

			// Assert - The client is created successfully, exception will be thrown later when gRPC is used
			Assert.NotNull(portfolioClient);
		}

		[Fact]
		public async Task DatabaseOperations_ShouldWorkWithRealSqliteConnection()
		{
			// Arrange
			using var context = new DatabaseContext(_contextOptions);
			
			// Verify we can perform basic database operations
			await context.Database.EnsureCreatedAsync();
			
			// Act - Try to query tables (should not throw)
			var tableCount = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM sqlite_master WHERE type='table'").FirstOrDefaultAsync();

			// Assert - Should have some tables created by EF migrations
			Assert.True(tableCount > 0, "Database should contain tables after EF migrations");
		}

		[Fact]
		public void DeserializeData_ShouldWorkWithRealWorldData()
		{
			// Arrange - Test with data that might come from a real API
			var realWorldData = new List<Dictionary<string, object>>
			{
				new Dictionary<string, object>
				{
					{ "Id", "550e8400-e29b-41d4-a716-446655440000" },
					{ "Symbol", "AAPL" },
					{ "Name", "Apple Inc." },
					{ "Price", 175.43 },
					{ "Volume", 50000000 },
					{ "LastUpdate", "2024-01-15T15:30:00.000Z" },
					{ "Active", true },
					{ "Exchange", "NASDAQ" },
					{ "Metadata", new { Sector = "Technology", MarketCap = 2800000000000L } }
				},
				new Dictionary<string, object>
				{
					{ "Id", "550e8400-e29b-41d4-a716-446655440001" },
					{ "Symbol", "GOOGL" },
					{ "Name", "Alphabet Inc." },
					{ "Price", 142.87 },
					{ "Volume", 25000000 },
					{ "LastUpdate", "2024-01-15T15:30:00.000Z" },
					{ "Active", true },
					{ "Exchange", "NASDAQ" },
					{ "Metadata", new { Sector = "Technology", MarketCap = 1800000000000L } }
				}
			};

			var jsonData = JsonSerializer.Serialize(realWorldData);

			// Act
			var result = PortfolioClient.DeserializeData(jsonData);

			// Assert
			Assert.Equal(2, result.Count);
			
			// Verify first record
			Assert.Equal("550e8400-e29b-41d4-a716-446655440000", result[0]["Id"]);
			Assert.Equal("AAPL", result[0]["Symbol"]);
			Assert.Equal("Apple Inc.", result[0]["Name"]);
			Assert.Equal(175.43, result[0]["Price"]);
			// Volume might be parsed as long or double, so check both
			var volumeValue = result[0]["Volume"];
			if (volumeValue is long longVolume)
			{
				Assert.Equal(50000000L, longVolume);
			}
			else if (volumeValue is double doubleVolume)
			{
				Assert.Equal(50000000.0, doubleVolume);
			}
			else
			{
				Assert.Fail($"Unexpected type for Volume: {volumeValue?.GetType()}");
			}
			Assert.True((bool)result[0]["Active"], "First record should be marked as active");
			
			// Verify second record
			Assert.Equal("550e8400-e29b-41d4-a716-446655440001", result[1]["Id"]);
			Assert.Equal("GOOGL", result[1]["Symbol"]);
			Assert.Equal("Alphabet Inc.", result[1]["Name"]);
			Assert.Equal(142.87, result[1]["Price"]);
			// Volume might be parsed as long or double, so check both
			var volumeValue2 = result[1]["Volume"];
			if (volumeValue2 is long longVolume2)
			{
				Assert.Equal(25000000L, longVolume2);
			}
			else if (volumeValue2 is double doubleVolume2)
			{
				Assert.Equal(25000000.0, doubleVolume2);
			}
			else
			{
				Assert.Fail($"Unexpected type for Volume: {volumeValue2?.GetType()}");
			}
			Assert.True((bool)result[1]["Active"], "Second record should be marked as active");
		}

		[Fact]
		public void PortfolioClient_ShouldDisposeCorrectly()
		{
			// Arrange
			using var context = new DatabaseContext(_contextOptions);
			var dbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			dbContextFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(context);

			using var httpClient = new HttpClient { BaseAddress = new Uri("https://test.example.com") };
			
			var portfolioClient = new PortfolioClient(
				httpClient,
				_mockSqlitePersistence.Object,
				_mockCurrencyExchange.Object,
				dbContextFactory.Object,
				_mockSyncTrackingService.Object,
				_mockLogger.Object);

			// Act
			var action = portfolioClient.Dispose;

			// Assert - Should not throw
			action.Should().NotThrow();
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					// Dispose managed resources
					_connection?.Dispose();
				}

				_disposed = true;
			}
		}
	}
}