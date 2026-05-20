using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public class ExternalDataCacheService(DatabaseContext dbContext)
	{
		public async Task<T?> GetOrAddAsync<T>(string cacheKey, string dataType, Func<Task<T>> factory, TimeSpan? expiration = null)
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

			T? value = default;
			try
			{
				value = await factory();
			}
			catch
			{
				// Do not cache failed results, let the exception bubble up if needed
				throw;
			}
			if (value != null)
			{
				string dataJson = JsonSerializer.Serialize(value);
				DateTime? expiresAt = expiration.HasValue ? now.Add(expiration.Value) : null;
				var newEntry = new ExternalDataCacheEntry
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
