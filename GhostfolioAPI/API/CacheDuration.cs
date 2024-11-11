using Microsoft.Extensions.Caching.Memory;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public static class CacheDuration
	{
		public static MemoryCacheEntryOptions? Long()
		{
			return new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24));
		}

		public static MemoryCacheEntryOptions? Short()
		{
			return new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
		}
	}
}
