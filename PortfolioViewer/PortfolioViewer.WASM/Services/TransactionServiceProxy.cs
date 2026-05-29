using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either the local-DB or API-backed <see cref="ITransactionService"/> based on
	/// <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class TransactionServiceProxy(
		[FromKeyedServices(DataSourceKeys.Local)] ITransactionService localService,
		[FromKeyedServices(DataSourceKeys.Api)] ITransactionService apiService,
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
