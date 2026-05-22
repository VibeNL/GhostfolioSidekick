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
		private enum TestCacheDataType
		{
			TestType,
			ExpireType
		}

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
			int value = 42;
			var result1 = await _cacheService.GetOrAddAsync<int>(Source.Yahoo, TypeOfData.SymbolProfile, key, () => Task.FromResult(value));
			Assert.Equal(value, result1);

			// Should retrieve from cache, not factory
			var result2 = await _cacheService.GetOrAddAsync<int>(Source.Yahoo, TypeOfData.SymbolProfile, key, () => Task.FromResult(99));
			Assert.Equal(value, result2);
		}

		public void Dispose()
		{
			_dbContext.Dispose();
			_connection.Dispose();
		}
	}
}
