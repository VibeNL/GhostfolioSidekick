namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public interface IExternalDataCacheService
	{
		Task<T?> GetOrAddAsync<T, TDataType>(string cacheKey, TDataType dataType, Func<Task<T>> factory, TimeSpan expiration) where TDataType : Enum;
	}
}