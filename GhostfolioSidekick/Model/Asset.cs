namespace GhostfolioSidekick.Model
{
	public class Asset
	{
		public Asset(Currency currency, string symbol, string name, string dataSource, string assetSubClass, string assetClass)
		{
			Currency = currency;
			Symbol = symbol;
			Name = name;
			DataSource = dataSource;
			AssetSubClass = assetSubClass;
			AssetClass = assetClass;
		}

		public Currency Currency { get; set; }

		public string Symbol { get; set; }

		public string Name { get; set; }

		public string DataSource { get; set; }

		public string AssetSubClass { get; set; }

		public string AssetClass { get; set; }
	}
}