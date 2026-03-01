using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ApiBrokerConfiguration
	{
		[JsonPropertyName("type")]
		public required string Type { get; set; }

		[JsonPropertyName("account")]
		public required string AccountName { get; set; }

		[JsonPropertyName("options")]
		public Dictionary<string, string>? Options { get; set; }
	}
}
