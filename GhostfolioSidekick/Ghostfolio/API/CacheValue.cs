using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	internal class CacheValue
	{
		public CacheValue(SymbolProfile? asset)
		{
			Asset = asset;
		}

		public SymbolProfile? Asset { get; private set; }
	}
}