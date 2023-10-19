using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ManualSymbolConfiguration
	{
		[JsonPropertyName("currency")]
		public string Currency { get; set; }


		[JsonPropertyName("isin")]
		public string ISIN { get; set; }


		[JsonPropertyName("name")]
		public string Name { get; set; }


		[JsonPropertyName("assetSubClass")]
		public string AssetSubClass { get; set; }


		[JsonPropertyName("assetClass")]
		public string AssetClass { get; set; }
	}
}