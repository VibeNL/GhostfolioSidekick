using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface ITransactionService
	{
		/// <summary>
		/// Loads transactions with pagination support
		/// </summary>
		/// <param name="parameters">Query parameters for filtering, sorting, and pagination</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Paginated transaction result</returns>
		Task<PaginatedTransactionResult> GetTransactionsPaginatedAsync(
			TransactionQueryParameters parameters,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all available transaction types from the database
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>List of unique transaction types</returns>
		Task<List<string>> GetTransactionTypesAsync(CancellationToken cancellationToken = default);
	}
}
