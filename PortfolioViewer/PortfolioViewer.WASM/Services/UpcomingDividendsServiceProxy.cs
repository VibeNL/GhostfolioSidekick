using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either the local-DB or API-backed <see cref="IUpcomingDividendsService"/> based on
	/// <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class UpcomingDividendsServiceProxy(
		[FromKeyedServices(DataSourceKeys.Local)] IUpcomingDividendsService localService,
		[FromKeyedServices(DataSourceKeys.Api)] IUpcomingDividendsService apiService,
		IDataSourceService dataSourceService) : IUpcomingDividendsService
	{
		private IUpcomingDividendsService Active =>
			dataSourceService.UseApiDirectly ? apiService : localService;

		public Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
			=> Active.GetUpcomingDividendsAsync();
	}
}
