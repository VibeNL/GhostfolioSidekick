using System.Text.Json.Serialization;

namespace GhostfolioSidekick.BrokerAPIs.TradeRepublic.Models
{
	internal sealed class PostboxResponse
	{
		[JsonPropertyName("items")]
		public List<PostboxItem>? Items { get; set; }

		[JsonPropertyName("cursors")]
		public PostboxCursors? Cursors { get; set; }
	}

	internal sealed class PostboxCursors
	{
		[JsonPropertyName("after")]
		public string? After { get; set; }
	}
}
