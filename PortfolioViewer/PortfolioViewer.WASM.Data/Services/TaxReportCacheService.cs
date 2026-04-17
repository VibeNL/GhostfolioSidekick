using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
    /// <summary>
    /// Caches the tax report result for the current calendar day.
    /// Registered as a singleton so the result survives page navigation within the same session.
    /// </summary>
    public sealed class TaxReportCacheService : ITaxReportCacheService
    {
        public List<TaxReportRow>? CachedResult { get; private set; }
        public DateOnly CachedAt { get; private set; }

        public bool IsValid =>
            CachedResult != null && CachedAt == DateOnly.FromDateTime(DateTime.Today);

        public void Store(List<TaxReportRow> result)
        {
            CachedResult = result;
            CachedAt = DateOnly.FromDateTime(DateTime.Today);
        }

        public void Invalidate()
        {
            CachedResult = null;
        }
    }
}
