using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either <see cref="DataIssuesService"/> (local DB) or
	/// <see cref="ApiDataIssuesService"/> (server API) based on <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class DataIssuesServiceProxy(
		DataIssuesService localService,
		ApiDataIssuesService apiService,
		IDataSourceService dataSourceService) : IDataIssuesService
	{
		private IDataIssuesService Active =>
			dataSourceService.UseApiDirectly ? apiService : localService;

		public Task<List<DataIssueDisplayModel>> GetActivitiesWithoutHoldingsAsync(CancellationToken cancellationToken = default)
			=> Active.GetActivitiesWithoutHoldingsAsync(cancellationToken);
	}
}
