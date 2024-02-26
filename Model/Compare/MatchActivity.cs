using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.SpecificActivity;

namespace GhostfolioSidekick.Model.Compare
{
	public class MatchActivity
	{
		public required IActivity Activity { get; set; }

		public bool IsMatched { get; set; }
	}
}
