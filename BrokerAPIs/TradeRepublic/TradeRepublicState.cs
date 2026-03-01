using System.Text.Json.Serialization;

namespace GhostfolioSidekick.BrokerAPIs.TradeRepublic
{
	internal sealed class TradeRepublicState
	{
		[JsonPropertyName("sessionToken")]
		public string? SessionToken { get; set; }

		[JsonPropertyName("downloadedDocumentIds")]
		public HashSet<string> DownloadedDocumentIds { get; set; } = [];
	}
}
