using System.Text.Json.Serialization;

namespace GhostfolioSidekick.BrokerAPIs.TradeRepublic.Models
{
	internal sealed class TradeRepublicLoginResponse
	{
		[JsonPropertyName("sessionToken")]
		public string? SessionToken { get; set; }

		[JsonPropertyName("numbersToCombine")]
		public int[]? NumbersToCombine { get; set; }
	}
}
