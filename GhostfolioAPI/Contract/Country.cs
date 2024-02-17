namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	public class Country
	{
		public required string Code { get; set; }

		public required decimal Weight { get; set; }

		public required string Continent { get; set; }

		public required string Name { get; set; }
	}
}
