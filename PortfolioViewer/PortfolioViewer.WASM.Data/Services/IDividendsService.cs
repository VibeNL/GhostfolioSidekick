using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface IDividendsService
	{
		Task<List<DividendModel>> GetDividendsAsync(DateOnly? startDate = null, DateOnly? endDate = null);
	}
}
