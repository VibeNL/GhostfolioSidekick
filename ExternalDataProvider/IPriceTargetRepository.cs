using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface IPriceTargetRepository
	{
		Task SavePriceTargetsAsync(IEnumerable<PriceTarget> priceTargets, string symbol, CancellationToken cancellationToken = default);

		Task ClearPriceTargetsAsync(string symbol, CancellationToken cancellationToken = default);

		Task<IEnumerable<PriceTarget>> GetPriceTargetsAsync(SymbolProfile symbol, CancellationToken cancellationToken = default);
	}
}
