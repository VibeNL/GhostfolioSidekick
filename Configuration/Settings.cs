using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	[ExcludeFromCodeCoverage]
	[method: SetsRequiredMembers]
	public class Settings()
	{
		[JsonPropertyName("delete.unused.symbols")]
		public bool DeleteUnusedSymbols { get; set; } = true;

		[JsonPropertyName("dataprovider.preference.order")]
		public required string RawDataProviderPreference { get; set; } = "YAHOO;COINGECKO";

		[JsonPropertyName("performance.currencies")]
		public required string RawCurrencies { get; set; } = "EUR;USD";

		public string[] DataProviderPreference => SplitConfigurationValue(RawDataProviderPreference);

		public string[] Currencies => SplitConfigurationValue(RawCurrencies);

		private static string[] SplitConfigurationValue(string value) =>
			[.. value.Split([';', ','], System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToUpperInvariant())];
	}
}