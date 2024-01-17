namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	public class Account
	{
		public required string Name { get; set; }

		public required string Id { get; set; }

		public decimal Balance { get; set; }

		public required string Currency { get; set; }

		public string? Comment { get; set; }

		public bool IsExcluded { get; set; }

		public string? PlatformId { get; set; }
	}
}