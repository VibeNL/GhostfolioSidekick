namespace GhostfolioSidekick.GhostfolioAPI.Cache;

public interface IGhostfolioSyncCacheService
{
	Task<T?> GetOrAddAsync<T>(string key, TimeSpan expiry, Func<Task<T?>> factory, CancellationToken cancellationToken = default);
}
