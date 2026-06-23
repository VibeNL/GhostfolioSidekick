using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.GhostfolioAPI.Cache;

/// <summary>
/// DB-backed cache for Ghostfolio API responses to reduce redundant REST calls.
/// Reuses the shared DbBackedCacheService.
/// </summary>
public class GhostfolioSyncCacheService(DbBackedCacheService cacheService) : IGhostfolioSyncCacheService
{
	/// <summary>
	/// Gets a cached Ghostfolio API response or fetches it using the factory function.
	/// </summary>
	public async Task<T?> GetOrAddAsync<T>(string key, TimeSpan expiry, Func<Task<T?>> factory, CancellationToken cancellationToken = default)
	{
		return await cacheService.GetOrAddAsync(key, expiry, factory, cancellationToken);
	}
}
