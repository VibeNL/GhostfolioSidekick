namespace GhostfolioSidekick.Ghostfolio.Contract
{
	public class MarketDataList
	{
		public List<MarketData> MarketData { get; set; }

		public SymbolProfile AssetProfile { get; set; }
	}
}
