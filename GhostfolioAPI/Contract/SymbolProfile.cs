using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	public class SymbolProfile
	{
		public required string Currency { get; set; }

		public required string Symbol { get; set; }

		public required string DataSource { get; set; }

		public required string Name { get; set; }

		public string? AssetSubClass { get; set; }

		public required string AssetClass { get; set; }

		public string? ISIN { get; set; }

		public int ActivitiesCount { get; set; }

		public IDictionary<string, string>? SymbolMapping { get; set; }

		public ScraperConfiguration? ScraperConfiguration { get; set; }

		public string? Comment { get; set; }

		public required Country[] Countries { get; set; }

		public required Sector[] Sectors { get; set; }

		internal static SymbolProfile Empty(Currency currency, string? name)
		{
			return new SymbolProfile()
			{
				Name = name ?? string.Empty,
				Symbol = name ?? string.Empty,
				Currency = currency.Symbol,
				AssetClass = string.Empty,
				DataSource = Datasource.MANUAL.ToString(),
			};
		}

		public override string ToString()
		{
			return $"{Symbol} {DataSource}";
		}
	}
}