using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public interface ISyncConfigurationService
    {
        Currency TargetCurrency { get; set; }
        Task<bool> StartSyncWithCurrencyAsync(Currency targetCurrency);
        event EventHandler<Currency>? CurrencyChanged;
    }
}