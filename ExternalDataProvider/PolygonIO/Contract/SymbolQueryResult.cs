namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO.Contract
{
	public class SymbolQueryResult
	{
		public int Count { get; set; }
			
		public string NextUrlstring { get; set; }

		public List<Symbol> Results { get; set; }
	}
}