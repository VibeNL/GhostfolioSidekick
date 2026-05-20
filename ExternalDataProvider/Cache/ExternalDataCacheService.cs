using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public class ExternalDataCacheService(DatabaseContext dbContext)
	{
		public async Task<T?> GetOrAddAsync<T>(string cacheKey, string dataType, Func<Task<T>> factory, TimeSpan expiration)
		{
			DateTime now = DateTime.UtcNow;
			ExternalDataCacheEntry? entry = await dbContext.ExternalDataCacheEntries
				.Where(e => e.CacheKey == cacheKey && e.DataType == dataType && (e.ExpiresAt == null || e.ExpiresAt > now))
				.FirstOrDefaultAsync();

			if (entry != null)
			{
				try
				{
					return JsonSerializer.Deserialize<T>(entry.DataJson);
				}
				catch
				{
					// ignore corrupt cache
				}
			}

			T? value = await factory();
			if (value != null)
			{
				string dataJson = JsonSerializer.Serialize(value);
				DateTime? expiresAt = now.Add(expiration);
				ExternalDataCacheEntry newEntry = new()
				{
					CacheKey = cacheKey,
					DataType = dataType,
					DataJson = dataJson,
					CreatedAt = now,
					ExpiresAt = expiresAt
				};

				// Remove old entries for this key/type
				dbContext.ExternalDataCacheEntries.RemoveRange(dbContext.ExternalDataCacheEntries.Where(e => e.CacheKey == cacheKey && e.DataType == dataType));
				_ = dbContext.ExternalDataCacheEntries.Add(newEntry);
				_ = await dbContext.SaveChangesAsync();
			}
			return value;
		}
	}
}
