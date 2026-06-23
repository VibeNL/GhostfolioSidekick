namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public interface IExternalDataCacheService
	{
		Task<T?> GetOrAddAsync<T>(string key, TimeSpan expiry, Func<Task<T>> factory, CancellationToken cancellationToken = default);
	}
}