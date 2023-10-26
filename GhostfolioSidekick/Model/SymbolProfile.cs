namespace GhostfolioSidekick.Model
{
	public class SymbolProfile
	{
		public Currency Currency { get; set; }

		public string Symbol { get; set; }

		public string DataSource { get; set; }

		public int ActivitiesCount { get; set; }

		public MarketDataMappings Mappings { get; private set; } = new MarketDataMappings();
	}
}