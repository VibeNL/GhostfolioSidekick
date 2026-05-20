using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public class ExternalDataCacheService(DatabaseContext dbContext)
	{

		/// <summary>
		/// Gets a cached value or adds it if not present. Removes expired and duplicate entries.
		/// </summary>
		public async Task<T?> GetOrAddAsync<T, TDataType>(string cacheKey, TDataType dataType, Func<Task<T>> factory, TimeSpan expiration) where TDataType : Enum
		{
			DateTime now = DateTime.UtcNow;

			// Remove all expired cache entries before proceeding
			_ = await dbContext.ExternalDataCacheEntries.Where(e => e.ExpiresAt != null && e.ExpiresAt <= now).ExecuteDeleteAsync();

			// Try to get a valid (not expired) cache entry
			string dataTypeString = $"{typeof(TDataType).FullName}.{dataType}";
			ExternalDataCacheEntry? entry = await dbContext.ExternalDataCacheEntries
				.Where(e => e.CacheKey == cacheKey && e.DataType == dataTypeString && (e.ExpiresAt == null || e.ExpiresAt > now))
				.FirstOrDefaultAsync();

			if (entry != null)
			{
				try
				{
					// Decompress and deserialize
					using MemoryStream ms = new(entry.DataJson);
					using GZipStream gzip = new(ms, CompressionMode.Decompress);
					using StreamReader reader = new(gzip, Encoding.UTF8);
					string json = await reader.ReadToEndAsync();
					return JsonSerializer.Deserialize<T>(json);
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
				byte[] compressed;
				using (MemoryStream ms = new())
				{
					using (GZipStream gzip = new(ms, CompressionLevel.Optimal, leaveOpen: true))
					using (StreamWriter writer = new(gzip, Encoding.UTF8))
					{
						await writer.WriteAsync(dataJson);
					}

					compressed = ms.ToArray();
				}

				DateTime? expiresAt = now.Add(expiration);
				ExternalDataCacheEntry newEntry = new()
				{
					CacheKey = cacheKey,
					DataType = dataTypeString,
					DataJson = compressed,
					CreatedAt = now,
					ExpiresAt = expiresAt
				};

				// Remove old entries for this key/type
				dbContext.ExternalDataCacheEntries.RemoveRange(dbContext.ExternalDataCacheEntries.Where(e => e.CacheKey == cacheKey && e.DataType == dataTypeString));
				_ = dbContext.ExternalDataCacheEntries.Add(newEntry);
				_ = await dbContext.SaveChangesAsync();
			}
			return value;
		}
	}
}
