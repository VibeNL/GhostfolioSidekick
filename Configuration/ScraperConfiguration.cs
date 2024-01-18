using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ScraperConfiguration
	{
		[JsonPropertyName("url")]
		public required string Url { get; set; }

		[JsonPropertyName("selector")]
		public required string Selector { get; set; }

		[JsonPropertyName("locale")]
		public string? Locale { get; set; }
	}
}