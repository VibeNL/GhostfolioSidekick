using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Model.Symbols
{
	public sealed class SymbolProfile(
		string symbol,
		string name,
		Currency currency,
		string dataSource,
		AssetClass assetClass,
		AssetSubClass? assetSubClass) : IEquatable<SymbolProfile>
	{
		private string? comment;

		public Currency Currency { get; set; } = currency;

		public string Symbol { get; set; } = symbol;

		public string Name { get; set; } = name;

		public string DataSource { get; set; } = dataSource;

		public AssetClass AssetClass { get; set; } = assetClass;

		public AssetSubClass? AssetSubClass { get; set; } = assetSubClass;

		public string? ISIN { get; set; }

		public MarketDataMappings Mappings { get; private set; } = new MarketDataMappings();

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

		public bool Equals(SymbolProfile? other)
		{
			return
				Currency.Symbol == other?.Currency.Symbol &&
				Name == other?.Name &&
				Symbol == other?.Symbol &&
				AssetClass == other?.AssetClass &&
				AssetSubClass == other.AssetSubClass;
		}

		private void ParseIdentifiers()
		{
			if (comment == null)
			{
				return;
			}

			var pattern = @"Known Identifiers: \[(.*?)\]";
			var match = Regex.Match(comment, pattern);
			var ids = (match.Groups.Count > 1 ? match.Groups[1]?.Value : null) ?? string.Empty;

			if (string.IsNullOrEmpty(ids))
			{
				return;
			}

			Identifiers.Clear();
			Identifiers.AddRange(ids.Split(','));
		}
	}
}