namespace GhostfolioSidekick.Model
{
	public class MarketData
	{
		public MarketData(string symbol, int activitiesCount, string trackinsight)
		{
			Symbol = symbol;
			ActivitiesCount = activitiesCount;
			Mappings.TrackInsight = trackinsight;
		}

		public int ActivitiesCount { get; set; }

		public MarketDataMappings Mappings { get; private set; } = new MarketDataMappings();

		public string Symbol { get; set; }
		public string V { get; }
	}
}