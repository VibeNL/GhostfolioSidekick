using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	internal class CacheKey : IEquatable<CacheKey>
	{
		private readonly string[] identifiers;
		private readonly AssetClass[] expectedAssetClass;
		private readonly AssetSubClass[] expectedAssetSubClass;

		public CacheKey(string[] identifiers, AssetClass[]? expectedAssetClass, AssetSubClass[]? expectedAssetSubClass)
		{
			this.identifiers = identifiers;
			this.expectedAssetClass = expectedAssetClass ?? [];
			this.expectedAssetSubClass = expectedAssetSubClass ?? [];
		}

		private string CompareString
		{
			get
			{
				var a = string.Join(",", identifiers);
				var b = string.Join(",", expectedAssetClass.Select(x => x.ToString()));
				var c = string.Join(",", expectedAssetSubClass.Select(x => x.ToString()));
				var r = string.Join("|", a, b, c);
				return r;
			}
		}

		public bool Equals(CacheKey? other)
		{
			if (other == null)
			{
				return false;
			}

			return string.Equals(CompareString, other.CompareString, StringComparison.InvariantCultureIgnoreCase);
		}

		public override bool Equals(object? obj)
		{
			if (obj is not CacheKey)
			{
				return false;
			}

			return Equals((CacheKey)obj);
		}

		public override int GetHashCode()
		{
			return CompareString.GetHashCode();
		}
	}
}