using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface ITransactionService
	{
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
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Loads transactions with pagination support
		/// </summary>
		/// <param name="targetCurrency">Currency to convert values to</param>
		/// <param name="startDate">Start date filter</param>
		/// <param name="endDate">End date filter</param>
		/// <param name="accountId">Account filter (0 for all accounts)</param>
		/// <param name="symbol">Symbol filter (empty for all symbols)</param>
		/// <param name="transactionType">Transaction type filter (empty for all types)</param>
		/// <param name="searchText">Search text filter (empty for no search)</param>
		/// <param name="sortColumn">Column to sort by</param>
		/// <param name="sortAscending">Sort direction</param>
		/// <param name="pageNumber">Page number (1-based)</param>
		/// <param name="pageSize">Number of items per page</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Paginated transaction result</returns>
		Task<PaginatedTransactionResult> GetTransactionsPaginatedAsync(
			Currency targetCurrency,
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			string transactionType,
			string searchText,
			string sortColumn,
			bool sortAscending,
			int pageNumber,
			int pageSize,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets total count of transactions with filtering
		/// </summary>
		/// <param name="startDate">Start date filter</param>
		/// <param name="endDate">End date filter</param>
		/// <param name="accountId">Account filter (0 for all accounts)</param>
		/// <param name="symbol">Symbol filter (empty for all symbols)</param>
		/// <param name="transactionType">Transaction type filter (empty for all types)</param>
		/// <param name="searchText">Search text filter (empty for no search)</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Total count of filtered transactions</returns>
		Task<int> GetTransactionCountAsync(
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			string transactionType,
			string searchText,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all available transaction types from the database
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>List of unique transaction types</returns>
		Task<List<string>> GetTransactionTypesAsync(CancellationToken cancellationToken = default);
	}
}
