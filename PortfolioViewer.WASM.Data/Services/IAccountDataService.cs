using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface IAccountDataService
	{
		Task<List<Account>> GetAccountInfo();

		Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(Currency currency, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

		Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default);
	}
}
