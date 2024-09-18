namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO.Contract
{
	internal class TickerQueryResult
	{
		public string Ticker { get; set; }

		public int QueryCount { get; set; }

		public int ResultsCount { get; set; }

		public bool Adjusted { get; set; }

		public List<TickerResult> Results { get; set; }
	}
}