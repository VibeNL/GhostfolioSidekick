namespace GhostfolioSidekick.Model
{
	public class MarketData
	{
		public MarketData(string symbol, string dataSource, int activitiesCount, string trackinsight)
		{
			Symbol = symbol;
			DataSource = dataSource;
			ActivitiesCount = activitiesCount;
			Mappings.TrackInsight = trackinsight;
		}

		public int ActivitiesCount { get; set; }

		public MarketDataMappings Mappings { get; private set; } = new MarketDataMappings();

		public string Symbol { get; set; }

		public string DataSource { get; set; }
	}
}