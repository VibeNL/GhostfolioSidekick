namespace GhostfolioSidekick.PortfolioViewer.Model
{
	public class SymbolProfile
	{
		public string Symbol { get; set; }
		public string DataSource { get; set; }
		public Currency Currency { get; set; }
		public string AssetClass { get; set; }
		public string? AssetSubClass { get; set; }
		public ICollection<StockSplit> StockSplits { get; set; }
		public ICollection<MarketData> MarketData { get; set; }
	}
}
