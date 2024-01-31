using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class PlatformConfiguration
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("url")]
		public string? Url { get; set; }
	}
}