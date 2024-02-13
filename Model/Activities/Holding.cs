using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public class Holding
	{
		public Holding(SymbolProfile? symbolProfile)
		{
			SymbolProfile = symbolProfile;
		}

		public SymbolProfile? SymbolProfile { get; }

		public List<Activity> Activities { get; set; } = new List<Activity>();
	}
}
