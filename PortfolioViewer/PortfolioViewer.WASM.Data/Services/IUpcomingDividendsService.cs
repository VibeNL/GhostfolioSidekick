using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
    public interface IUpcomingDividendsService
    {
        Task<List<UpcomingDividendModel>> GetDividendsAsync(DateOnly? startDate = null, DateOnly? endDate = null);
    }
}
