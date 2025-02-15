namespace GhostfolioSidekick.Model.Symbols
{
	public record SectorWeight
	{
		public SectorWeight()
		{
			// EF Core
			Name = default!;
		}

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