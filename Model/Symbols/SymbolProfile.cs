using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.Model.Symbols
{
	public class SymbolProfile(
		string symbol,
		string name,
		Currency currency,
		Datasource dataSource,
		AssetClass assetClass,
		AssetSubClass? assetSubClass) : IEquatable<SymbolProfile>
	{
		public Currency Currency { get; set; } = currency;

		public string Symbol { get; set; } = symbol;

		public string Name { get; set; } = name;

		public Datasource DataSource { get; set; } = dataSource;

		public AssetClass AssetClass { get; set; } = assetClass;

		public AssetSubClass? AssetSubClass { get; set; } = assetSubClass;

		public string? ISIN { get; set; }

		public MarketDataMappings Mappings { get; private set; } = new MarketDataMappings();

		public ScraperConfiguration ScraperConfiguration { get; private set; } = new ScraperConfiguration();

		public List<string> Identifiers { get; } = [];

		public string? Comment { get; set; }

		public int ActivitiesCount { get; set; }

		public bool Equals(SymbolProfile? other)
		{
			return
				Currency.Symbol == other?.Currency.Symbol &&
				Name == other?.Name &&
				Symbol == other?.Symbol &&
				AssetClass == other?.AssetClass &&
				AssetSubClass == other?.AssetSubClass;
		}
	}
}