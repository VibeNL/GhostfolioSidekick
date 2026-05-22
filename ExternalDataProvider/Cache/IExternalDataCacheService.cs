namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public interface IExternalDataCacheService
	{
		Task<T?> GetOrAddAsync<T>(Source source, TypeOfData dataType, string cacheKey, Func<Task<T>> factory);
	}
}