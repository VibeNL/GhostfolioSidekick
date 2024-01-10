using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class SymbolConfiguration
	{
		[JsonPropertyName("symbol")]
		public required string Symbol { get; set; }

		[JsonPropertyName("trackinsight")]
		public string? TrackingInsightSymbol { get; set; }

		[JsonPropertyName("manualSymbolConfiguration")]
		public ManualSymbolConfiguration? ManualSymbolConfiguration { get; set; }
	}
}
