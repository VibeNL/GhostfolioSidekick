using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ScraperConfiguration
	{
		[JsonPropertyName("url")]
		public string Url { get; set; }

		[JsonPropertyName("selector")]
		public string Selector { get; set; }
	}
}