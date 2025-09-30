namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public interface IWakeLockService
    {
        Task<bool> RequestWakeLockAsync();
        Task<bool> ReleaseWakeLockAsync();
        Task<bool> IsWakeLockSupportedAsync();
        Task<bool> IsWakeLockActiveAsync();
    }
}