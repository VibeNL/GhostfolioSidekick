using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

public interface IPriceTargetsService
{
	Task<List<PriceTargetDisplayModel>> GetPriceTargetsAsync(CancellationToken cancellationToken = default);
	Task<PriceTargetDisplayModel?> GetPriceTargetForSymbolAsync(string symbol, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets current holdings joined with their analyst price targets, sorted by proximity percentage
	/// (CurrentPrice / AverageTarget * 100) in descending order (passed targets first).
	/// </summary>
	Task<List<HoldingPriceTargetDisplayModel>> GetHoldingsPriceTargetsAsync(CancellationToken cancellationToken = default);
}
