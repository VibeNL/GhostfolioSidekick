using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Activities.Comparer
{
	public class SymbolComparer : IEqualityComparer<SymbolProfile>
	{
		public bool Equals(SymbolProfile? x, SymbolProfile? y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;

			// Primary comparison: Symbol must match (case-insensitive)
			if (!string.Equals(x.Symbol, y.Symbol, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// Flexible asset class comparison - allow compatible asset classes
			return AreAssetClassesCompatible(x.AssetClass, x.AssetSubClass, y.AssetClass, y.AssetSubClass);
		}

		public int GetHashCode([DisallowNull] SymbolProfile obj)
		{
			// Use only the symbol for hash code to ensure symbols from different sources 
			// with compatible asset classes get the same hash
			return obj.Symbol?.ToUpperInvariant().GetHashCode() ?? 0;
		}

		private static bool AreAssetClassesCompatible(AssetClass assetClass1, AssetSubClass? assetSubClass1, AssetClass assetClass2, AssetSubClass? assetSubClass2)
		{
			// If either asset class is Undefined, they're considered compatible
			if (assetClass1 == AssetClass.Undefined || assetClass2 == AssetClass.Undefined)
			{
				return true;
			}

			// If asset classes are different (and neither is Undefined), they're incompatible
			if (assetClass1 != assetClass2)
			{
				return false;
			}

			// Asset classes are the same - check subclass compatibility
			if (assetSubClass1 == null || assetSubClass2 == null)
			{
				return true;
			}

			return assetSubClass1 == assetSubClass2 || assetSubClass1 == AssetSubClass.Undefined || assetSubClass2 == AssetSubClass.Undefined;
		}
	}
}