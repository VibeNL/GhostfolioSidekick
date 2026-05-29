using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either <see cref="TransactionService"/> (local DB) or
	/// <see cref="ApiTransactionService"/> (server API) based on <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class TransactionServiceProxy(
		TransactionService localService,
		ApiTransactionService apiService,
		IDataSourceService dataSourceService) : ITransactionService
	{
		private ITransactionService Active =>
			dataSourceService.UseApiDirectly ? apiService : localService;

		public Task<PaginatedTransactionResult> GetTransactionsPaginatedAsync(TransactionQueryParameters parameters, CancellationToken cancellationToken = default)
			=> Active.GetTransactionsPaginatedAsync(parameters, cancellationToken);

		public Task<List<string>> GetTransactionTypesAsync(CancellationToken cancellationToken = default)
			=> Active.GetTransactionTypesAsync(cancellationToken);
	}
}
