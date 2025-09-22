using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public class SyncConfigurationService : ISyncConfigurationService
    {
        private readonly ILogger<SyncConfigurationService> _logger;
        private Currency _targetCurrency = Currency.EUR;

        public SyncConfigurationService(ILogger<SyncConfigurationService> logger)
        {
            _logger = logger;
        }

        public Currency TargetCurrency 
        { 
            get => _targetCurrency;
            set
            {
                if (_targetCurrency != value)
                {
                    _targetCurrency = value;
                    CurrencyChanged?.Invoke(this, value);
                    _logger.LogInformation("Target currency changed to {Currency}", value.Symbol);
                }
            }
        }

        public event EventHandler<Currency>? CurrencyChanged;

        public async Task<bool> StartSyncWithCurrencyAsync(Currency targetCurrency)
        {
            try
            {
                TargetCurrency = targetCurrency;
                _logger.LogInformation("Starting sync with currency conversion to {Currency}", targetCurrency.Symbol);
                
				await ConvertToTargetCurrency(targetCurrency);

				_logger.LogInformation("Sync completed successfully with currency {Currency}", targetCurrency.Symbol);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync with currency {Currency}", targetCurrency.Symbol);
                return false;
            }
        }

		private async Task ConvertToTargetCurrency(Currency targetCurrency)
		{
			throw new NotImplementedException();
		}
	}
}