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
	}
}
