
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public interface IApiWrapper
	{
		Task<List<SymbolProfile>> GetSymbolProfile(string identifier, bool includeIndexes);
	}
}