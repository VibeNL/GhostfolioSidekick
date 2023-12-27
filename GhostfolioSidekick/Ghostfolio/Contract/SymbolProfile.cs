namespace GhostfolioSidekick.Ghostfolio.Contract
{
	public class SymbolProfile
	{
		public string Currency { get; set; }

		public string Symbol { get; set; }

		public string DataSource { get; set; }

		public string Name { get; set; }

		public string AssetSubClass { get; set; }

		public string AssetClass { get; set; }

		public string ISIN { get; set; }

		public int ActivitiesCount { get; set; }

		public IDictionary<string, string> SymbolMapping { get; set; }

		public string Comment { get; set; }

		public override string ToString()
		{
			return $"{Symbol} {DataSource}";
		}
	}
}