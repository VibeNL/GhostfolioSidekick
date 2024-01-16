using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public class PartialActivity(SymbolProfile symbolProfile)
    {
        public SymbolProfile SymbolProfile { get; set; } = symbolProfile;
    }
}