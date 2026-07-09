using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface IPriceTargetRepository
	{
		Task SavePriceTargetsAsync(IEnumerable<PriceTarget> priceTargets, string symbol);

		Task ClearPriceTargetsAsync(string symbol);

		Task<IEnumerable<PriceTarget>> GetPriceTargetsAsync(SymbolProfile symbol);
	}
}
