using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

public interface IPriceTargetsService
{
	Task<List<PriceTargetDisplayModel>> GetPriceTargetsAsync(CancellationToken cancellationToken = default);
	Task<PriceTargetDisplayModel?> GetPriceTargetForSymbolAsync(string symbol, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets current holdings joined with their analyst price targets, sorted by proximity
	/// (smallest absolute percentage difference between current price and average target first).
	/// </summary>
	Task<List<HoldingPriceTargetDisplayModel>> GetHoldingsPriceTargetsAsync(CancellationToken cancellationToken = default);
}
