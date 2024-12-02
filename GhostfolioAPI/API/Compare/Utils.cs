﻿using GhostfolioSidekick.Model.Symbols;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Sync
{
	public static class Utils
	{
		public static bool IsGeneratedSymbol(GhostfolioAPI.Contract.SymbolProfile assetProfile)
		{
			var guidRegex = new Regex("^(?:\\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\\}{0,1})$");
			return guidRegex.IsMatch(assetProfile.Symbol) && assetProfile.DataSource == Datasource.MANUAL;
		}
	}
}