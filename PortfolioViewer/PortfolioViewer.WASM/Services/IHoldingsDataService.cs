using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using System;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    /// <summary>
    /// Interface for portfolio data services. Implement this interface to provide real data to the Holdings page.
    /// </summary>
    public interface IHoldingsDataService
    {
        /// <summary>
        /// Loads all holdings for the current portfolio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>List of holdings with their current values and performance data</returns>
        Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the portfolio value history (time series)
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="startDate">Start date for the time series</param>
        /// <param name="endDate">End date for the time series</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of portfolio value history points</returns>
        Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }
}