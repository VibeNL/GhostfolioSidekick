using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class SymbolConfiguration
	{
		[JsonPropertyName("symbol")]
		public string Symbol { get; set; }

		[JsonPropertyName("trackinsight")]
		public string TrackingInsightSymbol { get; set; }
	}
}
