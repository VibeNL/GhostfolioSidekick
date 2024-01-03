using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class Settings
	{
		[JsonPropertyName("use.crypto.workaround.stakereward.as.dividends")]
		public bool CryptoWorkaroundStakeReward { get; set; }

		[JsonPropertyName("use.crypto.workaround.dust")]
		public bool CryptoWorkaroundDust { get; set; }
	}
}