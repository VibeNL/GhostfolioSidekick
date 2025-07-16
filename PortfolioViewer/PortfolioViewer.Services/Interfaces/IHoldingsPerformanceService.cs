using GhostfolioSidekick.PortfolioViewer.Services.Models;

namespace GhostfolioSidekick.PortfolioViewer.Services.Interfaces;

public interface IHoldingsPerformanceService
{
    Task<(List<HoldingPerformanceData> holdings, int totalCount)> GetHoldingsDataAsync(int pageSize = 100, int page = 1);
    Task<List<string>> GetAssetClassesAsync();
    Task<List<AssetClassDistributionItem>> GetAssetClassDistributionAsync();
    Task<List<ActiveHoldingItem>> GetMostActiveHoldingsAsync(int count = 10);
    Task<List<HoldingPerformanceData>> FilterHoldingsByAssetClassAsync(string? assetClass = null);
}