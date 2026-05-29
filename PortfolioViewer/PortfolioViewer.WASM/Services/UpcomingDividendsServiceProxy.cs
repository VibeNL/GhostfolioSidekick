using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either <see cref="UpcomingDividendsService"/> (local DB) or
	/// <see cref="ApiUpcomingDividendsService"/> (server API) based on <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class UpcomingDividendsServiceProxy(
		UpcomingDividendsService localService,
		ApiUpcomingDividendsService apiService,
		IDataSourceService dataSourceService) : IUpcomingDividendsService
	{
		private IUpcomingDividendsService Active =>
			dataSourceService.UseApiDirectly ? apiService : localService;

		public Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
			=> Active.GetUpcomingDividendsAsync();
	}
}
