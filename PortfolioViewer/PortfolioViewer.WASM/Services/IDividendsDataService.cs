using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public interface IDividendsDataService
    {
        /// <summary>
        /// Gets dividend data aggregated by month
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="startDate">Start date for filtering</param>
        /// <param name="endDate">End date for filtering</param>
        /// <param name="accountId">Account filter (0 for all accounts)</param>
        /// <param name="symbol">Symbol filter (empty for all symbols)</param>
        /// <param name="assetClass">Asset class filter (empty for all asset classes)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of monthly aggregated dividends</returns>
        Task<List<DividendAggregateDisplayModel>> GetMonthlyDividendsAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId = 0,
            string symbol = "",
            string assetClass = "",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dividend data aggregated by year
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="startDate">Start date for filtering</param>
        /// <param name="endDate">End date for filtering</param>
        /// <param name="accountId">Account filter (0 for all accounts)</param>
        /// <param name="symbol">Symbol filter (empty for all symbols)</param>
        /// <param name="assetClass">Asset class filter (empty for all asset classes)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of yearly aggregated dividends</returns>
        Task<List<DividendAggregateDisplayModel>> GetYearlyDividendsAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId = 0,
            string symbol = "",
            string assetClass = "",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets individual dividend records
        /// </summary>
        /// <param name="targetCurrency">Currency to convert values to</param>
        /// <param name="startDate">Start date for filtering</param>
        /// <param name="endDate">End date for filtering</param>
        /// <param name="accountId">Account filter (0 for all accounts)</param>
        /// <param name="symbol">Symbol filter (empty for all symbols)</param>
        /// <param name="assetClass">Asset class filter (empty for all asset classes)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of individual dividend records</returns>
        Task<List<DividendDisplayModel>> GetDividendsAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId = 0,
            string symbol = "",
            string assetClass = "",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available accounts
        /// </summary>
        /// <returns>List of accounts</returns>
        Task<List<Account>> GetAccountsAsync();

        /// <summary>
        /// Gets all unique symbols that have dividends
        /// </summary>
        /// <returns>List of symbols</returns>
        Task<List<string>> GetDividendSymbolsAsync();

        /// <summary>
        /// Gets all unique asset classes that have dividends
        /// </summary>
        /// <returns>List of asset classes</returns>
        Task<List<string>> GetDividendAssetClassesAsync();

        /// <summary>
        /// Gets the earliest dividend date
        /// </summary>
        /// <returns>The earliest date</returns>
        Task<DateOnly> GetMinDividendDateAsync();
    }
}