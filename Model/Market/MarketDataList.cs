using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Market
{
	public class MarketDataList
	{
		public required List<MarketData> MarketData { get; set; }

		public required SymbolProfile AssetProfile { get; set; }
	}
}