using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ManualSymbolConfiguration
	{
		[JsonPropertyName("currency")]
		public required string Currency { get; set; }

		[JsonPropertyName("isin")]
		public string? ISIN { get; set; }


		[JsonPropertyName("name")]
		public required string Name { get; set; }


		[JsonPropertyName("assetSubClass")]
		public string? AssetSubClass { get; set; }

		[JsonPropertyName("assetClass")]
		public required string AssetClass { get; set; }

		[JsonPropertyName("scraperConfiguration")]
		public ScraperConfiguration? ScraperConfiguration { get; set; }

		[JsonPropertyName("countries")]
		public Country[] Countries { get; set; } = [];

		[JsonPropertyName("sectors")]
		public Sector[] Sectors { get; set; } = [];
	}
}