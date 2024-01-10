using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Model
{
	public class SymbolProfile
	{
		private string[] identifiers = new string[0];
		private string? comment;

		public SymbolProfile(
			Currency currency,
			string symbol,
			string? isin,
			string name,
			string? dataSource,
			AssetClass assetClass,
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

		public string? DataSource { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public AssetClass AssetClass { get; set; }

		public string? ISIN { get; set; }

		public int ActivitiesCount { get; set; }

		public MarketDataMappings Mappings { get; private set; } = new MarketDataMappings();

		public ScraperConfiguration ScraperConfiguration { get; private set; } = new ScraperConfiguration();

		public string? Comment
		{
			get => comment;
			set
			{
				comment = value;
				ParseIdentifiers();
			}
		}


		public IEnumerable<string> Identifiers => identifiers;

		private void ParseIdentifiers()
		{
			if (comment == null)
			{
				return;
			}

			var pattern = @"Known Identifiers: \[(.*?)\]";
			var match = Regex.Match(comment, pattern);
			var ids = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;

			if (string.IsNullOrEmpty(ids))
			{
				return;
			}

			identifiers = ids.Split(',');
		}

		public void AddIdentifier(string identifier)
		{
			Comment = $"Known Identifiers: [{string.Join(",", identifiers.Union(new[] { identifier }))}]";
		}

		public override string ToString()
		{
			return $"{Symbol} {DataSource}";
		}
	}
}