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
			using (var ctx = new DatabaseContext(options))
			{
				ctx.Database.EnsureCreated();
			}

			var factory = new InMemoryDbContextFactory(options);
			return new ExternalDataCacheService(factory);
		}

		private sealed class InMemoryDbContextFactory(DbContextOptions<DatabaseContext> options) : IDbContextFactory<DatabaseContext>
		{
			public DatabaseContext CreateDbContext() => new(options);
		}
	}
}
