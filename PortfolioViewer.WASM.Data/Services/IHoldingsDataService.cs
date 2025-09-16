using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model.Accounts;
using System;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
    /// <summary>
    /// Interface for portfolio data services. Implement this interface to provide real data to the Holdings page.
    /// </summary>
    public interface IHoldingsDataService
    {
        /// <summary>
        /// Loads all holdings for the current portfolio
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>List of holdings with their current values and performance data</returns>
        Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads holdings for the current portfolio with optional account filtering
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="accountId">Account filter</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>List of holdings with their current values and performance data</returns>
        Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, int accountId, CancellationToken cancellationToken = default);
    }
}