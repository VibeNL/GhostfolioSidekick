using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
    public interface ITaxReportCacheService
    {
        List<TaxReportRow>? CachedResult { get; }
        DateOnly CachedAt { get; }
        bool IsValid { get; }
        void Store(List<TaxReportRow> result);
        void Invalidate();
    }
}
