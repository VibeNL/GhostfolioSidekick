using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

public interface IPriceTargetsService
{
	Task<List<PriceTargetDisplayModel>> GetPriceTargetsAsync(CancellationToken cancellationToken = default);
	Task<PriceTargetDisplayModel?> GetPriceTargetForSymbolAsync(string symbol, CancellationToken cancellationToken = default);
}
