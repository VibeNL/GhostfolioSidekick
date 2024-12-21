using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Market
{
	public class MarketDataProfile
	{
		public required List<MarketData> MarketData { get; set; }

		public required List<StockSplit> StockSplit { get; set; }

		public required SymbolProfile AssetProfile { get; set; }
	}
}