using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model.Accounts;
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
		/// Get the earliest date for which we have portfolio data.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token for the async operation</param>
		/// <returns>The date</returns>
		Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Loads the portfolio value history (time series)
		/// </summary>
		/// <param name="targetCurrency">Currency to convert values to</param>
		/// <param name="startDate">Start date for the time series</param>
		/// <param name="endDate">End date for the time series</param>
		/// <param name="accountId">Account filter</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>List of portfolio value history points</returns>
		Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads all available accounts
        /// </summary>
        /// <returns>List of accounts</returns>
        Task<List<Account>> GetAccountsAsync();

        /// <summary>
        /// Loads price history for a specific holding
        /// </summary>
        /// <param name="symbol">Symbol of the holding</param>
        /// <param name="startDate">Start date for the price history</param>
        /// <param name="endDate">End date for the price history</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of price history points</returns>
        Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
            string symbol,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads all transactions with filtering options
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="startDate">Start date filter</param>
        /// <param name="endDate">End date filter</param>
        /// <param name="accountId">Account filter (0 for all accounts)</param>
        /// <param name="symbol">Symbol filter (empty for all symbols)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of transaction display models</returns>
        Task<List<TransactionDisplayModel>> GetTransactionsAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId,
            string symbol,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads all unique symbols for filter dropdown
        /// </summary>
        /// <returns>List of symbol names</returns>
        Task<List<string>> GetSymbolsAsync();

        /// <summary>
        /// Loads symbols available in a specific account
        /// </summary>
        /// <param name="accountId">Account ID to filter by</param>
        /// <returns>List of symbol names available in the account</returns>
        Task<List<string>> GetSymbolsByAccountAsync(int accountId);

        /// <summary>
        /// Loads accounts that have activities for a specific symbol
        /// </summary>
        /// <param name="symbol">Symbol to filter by</param>
        /// <returns>List of accounts that have the specified symbol</returns>
        Task<List<Account>> GetAccountsBySymbolAsync(string symbol);

        /// <summary>
        /// Loads account value history for all accounts over time
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="startDate">Start date for the time series</param>
        /// <param name="endDate">End date for the time series</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of account value history points</returns>
        Task<List<AccountValueHistoryPoint>> GetAccountValueHistoryAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }
}