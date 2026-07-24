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

		/// <summary>
		/// Number of underlying ordinary shares represented by one unit of this symbol.
		/// Only relevant for ADR (American Depositary Receipt) / GDR (Global Depositary Receipt) symbols.
		/// Defaults to 1 (no conversion) when not specified.
		/// </summary>
		[JsonPropertyName("underlyingSharesPerReceipt")]
		public decimal? UnderlyingSharesPerReceipt { get; set; }

		[JsonPropertyName("scraperConfiguration")]
		public ScraperConfiguration? ScraperConfiguration { get; set; }

		[JsonPropertyName("countries")]
		public Country[] Countries { get; set; } = [];

		[JsonPropertyName("sectors")]
		public Sector[] Sectors { get; set; } = [];
	}
}