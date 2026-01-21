using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities
{
	public class PartialSymbolIdentifierComparer : IEqualityComparer<PartialSymbolIdentifier>
	{
		public bool Equals(PartialSymbolIdentifier? x, PartialSymbolIdentifier? y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;

			// Primary comparison: Identifier must match (case-insensitive)
			if (!string.Equals(x.Identifier?.Trim(), y.Identifier?.Trim(), StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}

			// Lenient asset class comparison - allow compatible asset classes
			return AreAssetClassListsCompatible(x.AllowedAssetClasses, y.AllowedAssetClasses) &&
				   AreAssetSubClassListsCompatible(x.AllowedAssetSubClasses, y.AllowedAssetSubClasses);
		}

		public int GetHashCode([DisallowNull] PartialSymbolIdentifier obj)
		{
			// Use only the identifier for hash code to ensure identifiers with compatible 
			// but different asset classes get the same hash for dictionary lookups
			return obj.Identifier?.Trim().ToUpperInvariant().GetHashCode() ?? 0;
		}

		private static bool AreAssetClassListsCompatible(List<AssetClass>? list1, List<AssetClass>? list2)
		{
			// If either list is null or empty, they're considered compatible
			if ((list1 is null || list1.Count == 0) || (list2 is null || list2.Count == 0))
			{
				return true;
			}

			// If either list contains Undefined, they're compatible
			if (list1.Contains(AssetClass.Undefined) || list2.Contains(AssetClass.Undefined))
			{
				return true;
			}

			// Check if lists have any overlap
			return list1.Intersect(list2).Any();
		}

		private static bool AreAssetSubClassListsCompatible(List<AssetSubClass>? list1, List<AssetSubClass>? list2)
		{
			// If either list is null or empty, they're considered compatible
			if ((list1 is null || list1.Count == 0) || (list2 is null || list2.Count == 0))
			{
				return true;
			}

			// If either list contains Undefined, they're compatible
			if (list1.Contains(AssetSubClass.Undefined) || list2.Contains(AssetSubClass.Undefined))
			{
				return true;
			}

			// Check if lists have any overlap
			return list1.Intersect(list2).Any();
		}
	}
}