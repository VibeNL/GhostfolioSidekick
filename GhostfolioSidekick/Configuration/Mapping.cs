using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class Mapping
	{
		[JsonPropertyName("type")]
		public MappingType MappingType { get; set; }

		[JsonPropertyName("source")]
		public required string Source { get; set; }

		[JsonPropertyName("target")]
		public required string Target { get; set; }
	}
}
