using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Symbols
{
	[ExcludeFromCodeCoverage]
	public static class Datasource
	{
		public static readonly string YAHOO = "YAHOO";

		public static readonly string COINGECKO = "COINGECKO";

		public static readonly string MANUAL = "MANUAL";

		public static readonly string GHOSTFOLIO = "GHOSTFOLIO";

		public static string GetUnderlyingDataSource(string dataSource)
		{
			if (!IsGhostfolio(dataSource))
			{
				return dataSource;
			}

			return dataSource.Substring(GHOSTFOLIO.Length + 1);
		}

		public static bool IsGhostfolio(string dataSource)
		{
			return dataSource.StartsWith(GHOSTFOLIO, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
