namespace GhostfolioSidekick.Database.Model
{
	public class SymbolProfile
	{
		public int Id { get; set; }

		public required Currency Currency { get; set; }

		public required string Symbol { get; set; }

		public required string Name { get; set; }

		public required string DataSource { get; set; }

		public AssetClass AssetClass { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public string? ISIN { get; set; }

		public StockSplitList? StockSplitList { get; set; }

		public StockPriceList? StockPricesList { get; set; }
		
		public IEnumerable<Activity> Activities { get; set; }
	}
}
