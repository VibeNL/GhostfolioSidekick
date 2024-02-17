using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class Country
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("code")]
		public required string Code { get; set; }

		[JsonPropertyName("continent")]
		public required string Continent { get; set; }

		[JsonPropertyName("weight")]
		public required decimal Weight { get; set; }
	}
}