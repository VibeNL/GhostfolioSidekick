using GhostfolioSidekick.Ghostfolio.API.Contract;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public class MatchRawActivity
	{
		public RawActivity Activity { get; set; }
		public bool IsMatched { get; set; }
	}
}
