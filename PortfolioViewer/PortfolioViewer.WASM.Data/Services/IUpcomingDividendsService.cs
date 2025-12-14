using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
    public interface IUpcomingDividendsService
    {
        Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync();
    }
}