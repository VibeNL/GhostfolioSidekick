using GhostfolioSidekick.Ghostfolio.Contract;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public class MatchActivity
	{
		public required Activity Activity { get; set; }

		public bool IsMatched { get; set; }
	}
}
