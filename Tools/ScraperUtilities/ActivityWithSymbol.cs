using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Tools.ScraperUtilities
{
	public class ActivityWithSymbol
	{
		public required Activity Activity { get; set; }

		public string? Symbol { get; set; }

		internal string? SymbolName { get; set; }
	}
}
