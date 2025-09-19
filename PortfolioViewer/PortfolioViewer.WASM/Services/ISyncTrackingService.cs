namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public interface ISyncTrackingService
    {
        Task<DateTime?> GetLastSyncTimeAsync();
        Task SetLastSyncTimeAsync(DateTime syncTime);
        Task<bool> HasEverSyncedAsync();
    }
}