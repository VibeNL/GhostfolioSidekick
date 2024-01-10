namespace GhostfolioSidekick.Ghostfolio.Contract
{
	public class Platform
	{
		public required string Name { get; set; }

		public string? Url { get; set; }

		public required string Id { get; set; }
	}
}