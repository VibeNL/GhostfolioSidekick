using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface IDataIssuesService
	{
		Task<List<DataIssueDisplayModel>> GetActivitiesWithoutHoldingsAsync(CancellationToken cancellationToken = default);
	}
}