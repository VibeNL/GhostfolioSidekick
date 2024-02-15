using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Model.Symbols
{
	public sealed class SymbolProfile : IEquatable<SymbolProfile>
	{
		private string? comment;

		public SymbolProfile(
			string symbol,
			string name,
			Currency currency,
			string dataSource,
			AssetClass assetClass,
			AssetSubClass? assetSubClass,
			string[] countries,
			string[] sectors)
		{
			Symbol = symbol;
			Name = name;
			Currency = currency;
			DataSource = dataSource;
			AssetClass = assetClass;
			AssetSubClass = assetSubClass;
			Countries = countries;
			Sectors = sectors;
		}

		public Currency Currency { get; set; }

		public string Symbol { get; set; }

		public string Name { get; set; }

		public string DataSource { get; set; }

		public AssetClass AssetClass { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public string? ISIN { get; set; }

		public MarketDataMappings Mappings { get; } = new MarketDataMappings();

		public ScraperConfiguration ScraperConfiguration { get; set; } = new ScraperConfiguration();

		public List<string> Identifiers { get; } = [];

		public string? Comment
		{
			get => comment;
			set
			{
				comment = value;
				ParseIdentifiers();
			}
		}

		public int ActivitiesCount { get; set; }
		
		public IEnumerable<string> Countries { get; set; }

		public IEnumerable<string> Sectors { get; set; }

		private void ParseIdentifiers()
		{
			if (comment == null)
			{
				return;
			}

			var pattern = @"Known Identifiers: \[(.*?)\]";
			var match = Regex.Match(comment, pattern);
			var ids = match.Groups.Count > 1 ? match.Groups[1].Value : null;

			if (string.IsNullOrEmpty(ids))
			{
				return;
			}

			Identifiers.Clear();
			Identifiers.AddRange(ids.Split(','));
		}
		
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
	}
}