using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public class ExternalDataCacheService(IDbContextFactory<DatabaseContext> dbContextFactory) : IExternalDataCacheService
	{
		/// <summary>
		/// Gets a cached value or adds it if not present using a raw string key and explicit expiry.
		/// </summary>
		public async Task<T?> GetOrAddAsync<T>(string key, TimeSpan expiry, Func<Task<T>> factory)
		{
			await using DatabaseContext dbContext = await dbContextFactory.CreateDbContextAsync();

			DateTime now = DateTime.UtcNow;

			// Remove all expired cache entries before proceeding
			_ = await dbContext.ExternalDataCacheEntries.Where(e => e.ExpiresAt <= now).ExecuteDeleteAsync();

			// Try to get a valid (not expired) cache entry
			ExternalDataCacheEntry? entry = await dbContext.ExternalDataCacheEntries
				.Where(e => e.Key == key && e.ExpiresAt > now)
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

			// Do not cache null results (e.g. transient failures such as auth errors)
			if (value is null)
			{
				return value;
			}

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

			ExternalDataCacheEntry newEntry = new()
			{
				Key = key,
				DataJson = compressed,
				CreatedAt = now,
				ExpiresAt = now.Add(expiry)
			};

			// Remove old entries for this key
			dbContext.ExternalDataCacheEntries.RemoveRange(dbContext.ExternalDataCacheEntries.Where(e => e.Key == key));
			_ = dbContext.ExternalDataCacheEntries.Add(newEntry);
			_ = await dbContext.SaveChangesAsync();

			return value;
		}
	}
}
