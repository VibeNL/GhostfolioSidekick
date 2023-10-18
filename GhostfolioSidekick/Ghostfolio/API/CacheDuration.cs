using Microsoft.Extensions.Caching.Memory;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public static class CacheDuration
	{
		public static MemoryCacheEntryOptions Long()
		{
			return new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
		}

		public static MemoryCacheEntryOptions Short()
		{
			return null; //new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1));
		}

		public static MemoryCacheEntryOptions None()
		{
			return null;// new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.Zero);
		}
	}
}
