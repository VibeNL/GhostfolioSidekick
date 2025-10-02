using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// Interface for portfolio data services. Implement this interface to provide real data to the Holdings page.
	/// </summary>
	public interface IHoldingsDataService
	{
		/// <summary>
		/// Get holding by symbol
		/// </summary>
		Task<HoldingDisplayModel?> GetHoldingAsync(string symbol, CancellationToken cancellationToken = default);

		/// <summary>
		/// Loads all holdings for the current portfolio
		/// </summary>
		/// <param name="targetCurrency">Currency to convert values to</param>
		/// <param name="cancellationToken">Cancellation token for the async operation</param>
		/// <returns>List of holdings with their current values and performance data</returns>
		Task<List<HoldingDisplayModel>> GetHoldingsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Loads holdings for the current portfolio with optional account filtering
		/// </summary>
		/// <param name="targetCurrency">Currency to convert values to</param>
		/// <param name="accountId">Account filter</param>
		/// <param name="cancellationToken">Cancellation token for the async operation</param>
		/// <returns>List of holdings with their current values and performance data</returns>
		Task<List<HoldingDisplayModel>> GetHoldingsAsync(int accountId, CancellationToken cancellationToken = default);

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
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default);

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
			DateOnly startDate,
			DateOnly endDate,
			int? accountId,
			CancellationToken cancellationToken = default);

	}
}