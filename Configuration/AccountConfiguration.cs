using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class AccountConfiguration
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("currency")]
		public required string Currency { get; set; }

		[JsonPropertyName("comment")]
		public string? Comment { get; set; }

		[JsonPropertyName("platform")]
		public string? Platform { get; set; }

		[JsonPropertyName("sync-activities")]
		public bool SyncActivities { get; set; } = true;

		[JsonPropertyName("sync-balance")]
		public bool SyncBalance { get; set; } = true;
	}
}