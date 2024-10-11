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
			Identifiers = new List<string>();
			MatchedPartialIdentifiers = new List<PartialSymbolIdentifier>();
		}

		[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "DDD")]
		public SymbolProfile(
			string symbol,
			string name,
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
			MatchedPartialIdentifiers = new List<PartialSymbolIdentifier>();
		}

		public Currency Currency { get; set; }

		public string Symbol { get; set; }

		public string Name { get; set; }

		public string DataSource { get; set; }

		public AssetClass AssetClass { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public string? ISIN { get; set; }

		public List<string> Identifiers { get; set; }

		public string? Comment { get; set; }

		public virtual ICollection<CountryWeight> CountryWeight { get; set; }

		public virtual ICollection<SectorWeight> SectorWeights { get; set; }

		public virtual ICollection<MarketData> MarketData { get; set; } = [];

		public virtual ICollection<PartialSymbolIdentifier> MatchedPartialIdentifiers { get; set; }

		public override string ToString()
		{
			return Symbol;
		}

		public void MergeKnownIdentifiers(ICollection<PartialSymbolIdentifier> newIds)
		{
			foreach (var id in newIds)
			{
				if (!MatchedPartialIdentifiers.Contains(id))
				{
					MatchedPartialIdentifiers.Add(id);
				}
			}
		}
	}
}