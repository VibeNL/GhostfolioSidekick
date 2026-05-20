using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public class ExternalDataCacheService(DatabaseContext dbContext)
	{

		/// <summary>
		/// Gets a cached value or adds it if not present. Removes expired and duplicate entries.
		/// </summary>
		public async Task<T?> GetOrAddAsync<T>(string cacheKey, string dataType, Func<Task<T>> factory, TimeSpan expiration)
		{
			DateTime now = DateTime.UtcNow;

			// Remove all expired cache entries before proceeding
			_ = await dbContext.ExternalDataCacheEntries.Where(e => e.ExpiresAt != null && e.ExpiresAt <= now).ExecuteDeleteAsync();

			// Try to get a valid (not expired) cache entry
			ExternalDataCacheEntry? entry = await dbContext.ExternalDataCacheEntries
				.Where(e => e.CacheKey == cacheKey && e.DataType == dataType && (e.ExpiresAt == null || e.ExpiresAt > now))
				.FirstOrDefaultAsync();

			if (entry != null)
			{
				try
				{
					return JsonSerializer.Deserialize<T>(entry.DataJson);
				}
				catch (JsonException)
				{
					// Ignore corrupt cache, proceed to factory
				}
			}

			// Generate new value and cache it
			T? value = await factory();
			if (!EqualityComparer<T>.Default.Equals(value, default!))
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
