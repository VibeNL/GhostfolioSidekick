using System.Diagnostics.CodeAnalysis;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ProcessingService.Activities.Comparer
{
	internal class SymbolComparer : IEqualityComparer<SymbolProfile>
	{
		public bool Equals(SymbolProfile? x, SymbolProfile? y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return GetSymbolString(x) == GetSymbolString(y);
		}

		public int GetHashCode([DisallowNull] SymbolProfile obj)
		{
			return GetSymbolString(obj).GetHashCode();
		}

		private static string GetSymbolString(SymbolProfile x)
		{
			return string.Join("|", $"{x.Symbol}|{x.DataSource}|{x.AssetClass}|{x.AssetSubClass}|{x.Currency?.Symbol}");
		}
	}
}