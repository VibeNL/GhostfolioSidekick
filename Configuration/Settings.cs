using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	[ExcludeFromCodeCoverage]
	public class Settings
	{
		[SetsRequiredMembers]
		public Settings()
		{
		}

		[JsonPropertyName("delete.unused.symbols")]
		public bool DeleteUnusedSymbols { get; set; } = true;

		[JsonPropertyName("dataprovider.preference.order")]
		public required string DataProviderPreference { get; set; } = "YAHOO;COINGECKO";

		[JsonPropertyName("performance.primarycurrency")]
		public required string PrimaryCurrency { get; set; } = "EUR";
	}
}