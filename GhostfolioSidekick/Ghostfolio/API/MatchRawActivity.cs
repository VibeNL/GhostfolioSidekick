using GhostfolioSidekick.Ghostfolio.API.Contract;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public partial class GhostfolioAPI
	{
		private class MatchRawActivity
		{
			public RawActivity Activity { get; set; }
			public bool IsMatched { get; set; }
		}
	}
}
