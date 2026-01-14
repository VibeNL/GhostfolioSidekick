using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Symbols
{
	public record class SymbolProfile
	{
		public SymbolProfile()
		{
			// EF Core
			Symbol = null!;
			Name = null!;
			Currency = null!;
			DataSource = null!;
			AssetClass = 0;
			AssetSubClass = null!;
			CountryWeight = null!;
			SectorWeights = null!;
			Identifiers = [];
		}

		public SymbolProfile(
			string symbol,
			string? name,
			List<string> identifiers,
			Currency currency,
			string dataSource,
			AssetClass assetClass,
			AssetSubClass? assetSubClass,
			CountryWeight[] countries,
			SectorWeight[] sectors)
		{
			Symbol = symbol;
			Name = name;
			Currency = currency;
			DataSource = dataSource;
			AssetClass = assetClass;
			AssetSubClass = assetSubClass;
			CountryWeight = countries;
			SectorWeights = sectors;
			Identifiers = identifiers;
		}

		public Currency Currency { get; set; }

		public string Symbol { get; set; }

		public string? Name { get; set; }

		public string DataSource { get; set; }

		public AssetClass AssetClass { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public string? ISIN { get; set; }

		public List<string> Identifiers { get; set; }

		public string? Comment { get; set; }

		public string? WebsiteUrl { get; set; }

		public virtual ICollection<CountryWeight> CountryWeight { get; set; }

		public virtual ICollection<SectorWeight> SectorWeights { get; set; }

		public virtual ICollection<MarketData> MarketData { get; set; } = [];

		public virtual ICollection<StockSplit> StockSplits { get; set; } = [];

		public virtual ICollection<Dividend> Dividends { get; set; } = [];
		
		public virtual PriceTarget? PriceTarget { get; set; }

		public override string ToString()
		{
			return Symbol;
		}
	}
}