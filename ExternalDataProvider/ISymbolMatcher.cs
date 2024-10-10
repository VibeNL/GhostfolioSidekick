using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface ISymbolMatcher
	{
		Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] identifiers);
	}
}
