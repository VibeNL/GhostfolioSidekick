using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class Mapping
	{
		[JsonPropertyName("type")]
		public MappingType MappingType { get; set; }

		[JsonPropertyName("source")]
		public string Source { get; set; }

		[JsonPropertyName("target")]
		public string Target { get; set; }
	}
}
