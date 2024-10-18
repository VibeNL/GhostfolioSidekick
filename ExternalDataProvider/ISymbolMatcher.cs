using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface ISymbolMatcher
	{
		string DataSource { get; }

		Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] identifiers);
	}
}
