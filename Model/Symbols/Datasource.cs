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

		public static readonly string DividendMax = "DIVIDENDMAX";

		public static string GetUnderlyingDataSource(string dataSource)
		{
			if (!IsGhostfolio(dataSource))
			{
				return dataSource;
			}

			return dataSource[(GHOSTFOLIO.Length + 1)..];
		}

		public static bool IsGhostfolio(string dataSource)
		{
			return dataSource.StartsWith(GHOSTFOLIO, StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Low means important, high means less important. This is used to determine which datasource to use when multiple datasources are available for a symbol.
		/// </summary>
		public static int GetPriority(string dataSource)
		{
			// Prefer Yahoo and CoinGecko
			if (dataSource.Equals(YAHOO, StringComparison.InvariantCultureIgnoreCase) ||
				dataSource.Equals(COINGECKO, StringComparison.InvariantCultureIgnoreCase))
			{
				return 1;
			}

			// Prefer manual data source
			if (dataSource.Equals(MANUAL, StringComparison.InvariantCultureIgnoreCase))
			{
				return 2;
			}

			// Prefer Ghostfolio data source
			if (IsGhostfolio(dataSource))
			{
				return 3;
			}

			// Default priority
			return 4;
		}
	}
}
