using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class AccountConfiguration
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; }

		[JsonPropertyName("comment")]
		public string? Comment { get; set; }

		[JsonPropertyName("platform")]
		public string? Platform { get; set; }
	}
}