using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either the local-DB or API-backed <see cref="IDataIssuesService"/> based on
	/// <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class DataIssuesServiceProxy(
		[FromKeyedServices(DataSourceKeys.Local)] IDataIssuesService localService,
		[FromKeyedServices(DataSourceKeys.Api)] IDataIssuesService apiService,
		IDataSourceService dataSourceService) : IDataIssuesService
	{
		private IDataIssuesService Active =>
			dataSourceService.UseApiDirectly ? apiService : localService;

		public Task<List<DataIssueDisplayModel>> GetActivitiesWithoutHoldingsAsync(CancellationToken cancellationToken = default)
			=> Active.GetActivitiesWithoutHoldingsAsync(cancellationToken);
	}
}
