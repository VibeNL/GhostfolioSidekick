namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO
{
	internal class CurrencyQueryResult
	{
		public string Ticker { get; set; }

		public int QueryCount { get; set; }

		public int ResultsCount { get; set; }

		public bool Adjusted { get; set; }

		public List<CurrencyTickerResult> Results { get; set; }
	}
}