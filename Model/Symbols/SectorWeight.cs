namespace GhostfolioSidekick.Model.Symbols
{
	public class SectorWeight
	{
		public SectorWeight(
			string name,
			decimal weight)
		{
			Weight = weight;
			Name = name;
		}

		public decimal Weight { get; set; }
		public string Name { get; set; }
	}
}