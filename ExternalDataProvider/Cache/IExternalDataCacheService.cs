namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public interface IExternalDataCacheService
	{
		Task<T?> GetOrAddAsync<T>(CacheKey cacheKey, Func<Task<T>> factory);
		Task<T?> GetOrAddAsync<T>(string key, TimeSpan expiry, Func<Task<T>> factory);
	}
}