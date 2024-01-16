namespace GhostfolioSidekick.Model
{
	public class SymbolProfile(
		string symbol,
		string name,
		Currency currency,
		string dataSource,
		AssetClass assetClass,
		AssetSubClass? assetSubClass)
	{
		public Currency Currency { get; set; } = currency;

		public string Symbol { get; set; } = symbol;

		public string Name { get; set; } = name;

		public string DataSource { get; set; } = dataSource;

		public AssetClass AssetClass { get; set; } = assetClass;

		public AssetSubClass? AssetSubClass { get; set; } = assetSubClass;

		public string? ISIN { get; set; }
	}
}