using System.Text.Json.Serialization;

namespace GhostfolioSidekick.BrokerAPIs.TradeRepublic.Models
{
	internal sealed class PostboxItem
	{
		[JsonPropertyName("id")]
		public string? Id { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("date")]
		public string? Date { get; set; }

		[JsonPropertyName("detail")]
		public PostboxItemDetail? Detail { get; set; }
	}

	internal sealed class PostboxItemDetail
	{
		[JsonPropertyName("action")]
		public PostboxAction? Action { get; set; }
	}

	internal sealed class PostboxAction
	{
		[JsonPropertyName("type")]
		public string? Type { get; set; }

		[JsonPropertyName("id")]
		public string? Id { get; set; }
	}
}
