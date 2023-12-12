using Microsoft.Extensions.Caching.Memory;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public static class CacheDuration
	{
		public static MemoryCacheEntryOptions? Long()
		{
			return None(); //return new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
		}

		public static MemoryCacheEntryOptions? Short()
		{
			return None(); //return new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
		}

		public static MemoryCacheEntryOptions? None()
		{
			return null;
		}
	}
}
