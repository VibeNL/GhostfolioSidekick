using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Cache
{

	public class ExternalDataCacheServiceTests : IDisposable
	{
		private enum TestCacheDataType
		{
			TestType,
			ExpireType
		}

		private readonly ExternalDataCacheService _cacheService;
		private readonly SqliteConnection _connection;

		public ExternalDataCacheServiceTests()
		{
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(_connection)
				.Options;
			using (var ctx = new DatabaseContext(options))
			{
				ctx.Database.EnsureCreated();
			}

			var factory = new TestDbContextFactory(options);
			var dbBackedCache = new DbBackedCacheService(factory);
			_cacheService = new ExternalDataCacheService(dbBackedCache);
		}

		private sealed class TestDbContextFactory(DbContextOptions<DatabaseContext> options) : IDbContextFactory<DatabaseContext>
		{
			public DatabaseContext CreateDbContext() => new(options);
		}

		[Fact]
		public async Task GetOrAddAsync_CachesAndRetrievesValue()
		{
			int value = 42;
			var result1 = await _cacheService.GetOrAddAsync<int>("test:key", TimeSpan.FromMinutes(5), () => Task.FromResult(value), TestContext.Current.CancellationToken);
			Assert.Equal(value, result1);

			// Should retrieve from cache, not factory
			var result2 = await _cacheService.GetOrAddAsync<int>("test:key", TimeSpan.FromMinutes(5), () => Task.FromResult(99), TestContext.Current.CancellationToken);
			Assert.Equal(value, result2);
		}

		public void Dispose()
		{
			_connection.Dispose();
		}
	}
}
