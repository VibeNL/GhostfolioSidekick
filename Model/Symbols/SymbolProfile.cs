using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Symbols
{
	public sealed class SymbolProfile : IEquatable<SymbolProfile>
	{
		internal SymbolProfile()
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

		public IEnumerable<CountryWeight> CountryWeight { get; set; }

		public IEnumerable<SectorWeight> SectorWeights { get; set; }

		public List<MarketData> MarketData { get; set; } = [];

		[ExcludeFromCodeCoverage]
		public bool Equals(SymbolProfile? other)
		{
			return
				Currency.Symbol == other?.Currency.Symbol &&
				Name == other?.Name &&
				Symbol == other?.Symbol &&
				AssetClass == other?.AssetClass &&
				AssetSubClass == other.AssetSubClass;
		}

		public static bool operator ==(SymbolProfile? left, SymbolProfile? right)
		{
			if (ReferenceEquals(left, null))
			{
				return ReferenceEquals(right, null);
			}

			return left.Equals(right);
		}

		public static bool operator !=(SymbolProfile? left, SymbolProfile? right)
		{
			return !(left == right);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as SymbolProfile);
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + Currency.Symbol.GetHashCode();
			hash = hash * 23 + Name.GetHashCode();
			hash = hash * 23 + Symbol.GetHashCode();
			hash = hash * 23 + AssetClass.GetHashCode();
			hash = hash * 23 + (AssetSubClass?.GetHashCode() ?? 0);
			return hash;
		}

		public override string ToString()
		{
			return Symbol;
		}
	}
}