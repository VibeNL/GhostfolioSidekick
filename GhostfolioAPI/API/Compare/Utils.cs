using GhostfolioSidekick.Model.Symbols;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.GhostfolioAPI.API.Compare
{
	public static partial class Utils
	{
		public static bool IsGeneratedSymbol(Contract.SymbolProfile assetProfile)
		{
			var guidRegex = GeneratedSymbolRegex();
			return guidRegex.IsMatch(assetProfile.Symbol) && assetProfile.DataSource == Datasource.MANUAL;
		}

		[GeneratedRegex("^(?:\\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\\}{0,1})$", RegexOptions.None, 1000)]
		private static partial Regex GeneratedSymbolRegex();
	}
}
