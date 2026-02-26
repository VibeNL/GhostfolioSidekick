using System.Text.Json.Serialization;

namespace GhostfolioSidekick.BrokerAPIs.TradeRepublic.Models
{
	internal sealed class PostboxDocumentDetail
	{
		[JsonPropertyName("url")]
		public string? Url { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }
	}
}
