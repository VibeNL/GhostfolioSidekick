using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either <see cref="HoldingsDataService"/> (local DB) or
	/// <see cref="ApiHoldingsDataService"/> (server API) based on <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class HoldingsDataServiceProxy(
		HoldingsDataService localService,
		ApiHoldingsDataService apiService,
		IDataSourceService dataSourceService) : IHoldingsDataService
	{
		private IHoldingsDataService Active =>
			dataSourceService.UseApiDirectly ? apiService : localService;

		public Task<HoldingDisplayModel?> GetHoldingAsync(string symbol, CancellationToken cancellationToken = default)
			=> Active.GetHoldingAsync(symbol, cancellationToken);

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(CancellationToken cancellationToken = default)
			=> Active.GetHoldingsAsync(cancellationToken);

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(int accountId, CancellationToken cancellationToken = default)
			=> Active.GetHoldingsAsync(accountId, cancellationToken);

		public Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
			=> Active.GetHoldingPriceHistoryAsync(symbol, startDate, endDate, cancellationToken);

		public Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
			DateOnly startDate,
			DateOnly endDate,
			int? accountId,
			CancellationToken cancellationToken = default)
			=> Active.GetPortfolioValueHistoryAsync(startDate, endDate, accountId, cancellationToken);
	}
}
