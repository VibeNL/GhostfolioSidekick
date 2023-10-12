namespace GhostfolioSidekick.Ghostfolio.API.Contract
{
	public class Market
	{
		public List<MarketData> MarketData { get; set; }

		public int ActivitiesCount { get; set; }

		public string Symbol { get; set; }

		public IDictionary<string, string> SymbolMapping { get; set; }
	}
}
