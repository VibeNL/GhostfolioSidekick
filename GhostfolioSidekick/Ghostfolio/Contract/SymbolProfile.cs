namespace GhostfolioSidekick.Ghostfolio.Contract
{
	public class SymbolProfile
	{
		public required string Currency { get; set; }

		public required string Symbol { get; set; }

		public string? DataSource { get; set; }

		public required string Name { get; set; }

		public string? AssetSubClass { get; set; }

		public required string AssetClass { get; set; }

		public string? ISIN { get; set; }

		public int ActivitiesCount { get; set; }

		public IDictionary<string, string>? SymbolMapping { get; set; }

		public string? Comment { get; set; }

		public override string ToString()
		{
			return $"{Symbol} {DataSource}";
		}
	}
}