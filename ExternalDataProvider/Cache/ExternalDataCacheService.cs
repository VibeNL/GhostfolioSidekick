using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public class ExternalDataCacheService(DbBackedCacheService cacheService) : IExternalDataCacheService
	{
		/// <summary>
		/// Gets a cached value or adds it if not present using a raw string key and explicit expiry.
		/// </summary>
		public async Task<T?> GetOrAddAsync<T>(string key, TimeSpan expiry, Func<Task<T>> factory)
		{
			return await cacheService.GetOrAddAsync(key, expiry, async () => await factory());
		}
	}
}
