using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class Sector
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("weight")]
		public required decimal Weight { get; set; }
	}
}