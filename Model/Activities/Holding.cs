using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public class Holding(SymbolProfile? symbolProfile)
	{
		public SymbolProfile? SymbolProfile { get; set; } = symbolProfile;

		public List<Activity> Activities { get; set; } = new List<Activity>();
	}
}
