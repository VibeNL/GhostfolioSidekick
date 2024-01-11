namespace GhostfolioSidekick.Ghostfolio.Contract
{
	public class MarketDataList
	{
		public required List<MarketData> MarketData { get; set; }

		public required SymbolProfile AssetProfile { get; set; }
	}
}
