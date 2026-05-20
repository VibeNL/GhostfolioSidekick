using System;
using System.Threading.Tasks;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Cache
{
	public class ExternalDataCacheServiceTests : IDisposable
	{
		private readonly DatabaseContext _dbContext;
		private readonly ExternalDataCacheService _cacheService;
		private readonly SqliteConnection _connection;

		public ExternalDataCacheServiceTests()
		{
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(_connection)
				.Options;
			_dbContext = new DatabaseContext(options);
			_dbContext.Database.EnsureCreated();
			_cacheService = new ExternalDataCacheService(_dbContext);
		}

		[Fact]
		public async Task GetOrAddAsync_CachesAndRetrievesValue()
		{
			string key = "test:key";
			string type = "TestType";
			int value = 42;
			var result1 = await _cacheService.GetOrAddAsync(key, type, () => Task.FromResult(value), TimeSpan.FromMinutes(5));
			Assert.Equal(value, result1);

			// Should retrieve from cache, not factory
			var result2 = await _cacheService.GetOrAddAsync(key, type, () => Task.FromResult(99), TimeSpan.FromMinutes(5));
			Assert.Equal(value, result2);
		}

	   [Fact]
	   public async Task GetOrAddAsync_ExpiresCache()
	   {
		   string key = "expire:key";
		   string type = "ExpireType";
		   int value = 123;
		   await _cacheService.GetOrAddAsync(key, type, () => Task.FromResult(value), TimeSpan.FromMilliseconds(10));
#if NET8_0_OR_GREATER
		   await Task.Delay(50, TestContext.Current.CancellationToken);
#else
		   await Task.Delay(50);
#endif
		   var result = await _cacheService.GetOrAddAsync(key, type, () => Task.FromResult(456), TimeSpan.FromMinutes(5));
		   Assert.Equal(456, result);
	   }

		public void Dispose()
		{
			_dbContext.Dispose();
			_connection.Dispose();
		}
	}
}
