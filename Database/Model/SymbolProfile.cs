namespace GhostfolioSidekick.Database.Model
{
	public class SymbolProfile
	{
		public required Currency Currency { get; set; }

		public required string Symbol { get; set; }

		public required string Name { get; set; }

		public required string DataSource { get; set; }

		public AssetClass AssetClass { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public string? ISIN { get; set; }
	}
}
