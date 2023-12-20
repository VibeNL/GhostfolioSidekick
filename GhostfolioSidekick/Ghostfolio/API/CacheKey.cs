using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	internal class CacheKey : IEquatable<CacheKey>
	{
		public string[] Identifiers { get; }
		private AssetClass?[] expectedAssetClass;
		private AssetSubClass?[] expectedAssetSubClass;

		public CacheKey(string[] identifiers, AssetClass?[] expectedAssetClass, AssetSubClass?[] expectedAssetSubClass)
		{
			Identifiers = identifiers;
			this.expectedAssetClass = expectedAssetClass;
			this.expectedAssetSubClass = expectedAssetSubClass;
		}

		public bool Equals(CacheKey? other)
		{
			// TODO
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as CacheKey);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}