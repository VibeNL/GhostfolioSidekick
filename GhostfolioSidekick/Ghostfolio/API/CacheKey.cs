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

		private string CompareString
		{
			get
			{
				var a = string.Join(",", Identifiers);
				var b = expectedAssetClass != null ? string.Join(",", expectedAssetClass.Select(x => x?.ToString())) : string.Empty;
				var c = expectedAssetSubClass != null ? string.Join(",", expectedAssetSubClass.Select(x => x?.ToString())) : string.Empty;
				var r = string.Join("|", a, b, c);
				return r;
			}
		}

		public bool Equals(CacheKey? other)
		{
			return string.Equals(this.CompareString, other.CompareString, StringComparison.InvariantCultureIgnoreCase);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as CacheKey);
		}

		public override int GetHashCode()
		{
			return CompareString.GetHashCode();
		}
	}
}