﻿namespace GhostfolioSidekick.Model
{
	public class SymbolProfile
	{
		public SymbolProfile()
		{
		}

		public SymbolProfile(
			Currency currency,
			string symbol,
			string isin,
			string name,
			string dataSource,
			AssetClass? assetClass,
			AssetSubClass? assetSubClass)
		{
			Currency = currency;
			Symbol = symbol;
			ISIN = isin;
			Name = name;
			DataSource = dataSource;
			AssetSubClass = assetSubClass;
			AssetClass = assetClass;
		}
		public Currency Currency { get; set; }

		public string Symbol { get; set; }

		public string Name { get; set; }

		public string DataSource { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public AssetClass? AssetClass { get; set; }

		public string ISIN { get; set; }

		public int ActivitiesCount { get; set; }

		public MarketDataMappings Mappings { get; private set; } = new MarketDataMappings();
	}
}