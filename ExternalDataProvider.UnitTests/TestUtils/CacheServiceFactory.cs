using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.TestUtils
{
	public static class CacheServiceFactory
	{
		public static ExternalDataCacheService CreateInMemoryCacheService()
		{
			var connection = new SqliteConnection("DataSource=:memory:");
			connection.Open();
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(connection)
				.Options;
			var dbContext = new DatabaseContext(options);
			dbContext.Database.EnsureCreated();
			return new ExternalDataCacheService(dbContext);
		}
	}
}
