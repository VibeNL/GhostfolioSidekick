using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	[ExcludeFromCodeCoverage]
	public class Settings : ISettings
	{
		[JsonPropertyName("delete.unused.symbols")]
		public bool DeleteUnusedSymbols { get; set; } = true;

		[JsonPropertyName("dataprovider.preference.order")]
		public required string DataProviderPreference { get; set; } = "YAHOO;COINGECKO";

		[JsonPropertyName("performance.primarycurrency")]
		public required string PrimaryCurrency { get; set; } = "EUR";

		/// <summary>
		/// When true, parsers skip transaction types they don't support (with a warning) instead of aborting the import. Default is false.
		/// </summary>
		[JsonPropertyName("parsers.skip.unknown.transaction.types")]
		public bool SkipUnknownTransactionTypes { get; set; } = false;
	}
}