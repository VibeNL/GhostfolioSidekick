using GhostfolioSidekick.Model;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Activities.Comparer
{
	internal class HoldingComparer : IEqualityComparer<Holding>
	{
		public bool Equals(Holding? x, Holding? y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return GetSymbolString(x) == GetSymbolString(y);
		}

		public int GetHashCode([DisallowNull] Holding obj)
		{
			return GetSymbolString(obj).GetHashCode();
		}

		private static string GetSymbolString(Holding holding)
		{
			return string.Join("|", holding.SymbolProfiles.OrderBy(x => x.Symbol).Select(x => $"{x.Symbol}|{x.DataSource}|{x.AssetClass}|{x.AssetSubClass}|{x.Currency.Symbol}")).ToLowerInvariant();
		}
	}
}
