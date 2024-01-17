using GhostfolioSidekick.GhostfolioAPI.Contract;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class MatchActivity
	{
		public required Activity Activity { get; set; }

		public bool IsMatched { get; set; }
	}
}
