using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Matches
{
	public class ActivitySymbol
	{
		public virtual Activity? Activity { get; set; }

		public virtual SymbolProfile? SymbolProfile { get; set; }

		public IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public int Id { get; set; }
	}
}
