using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface IAccountDataService
	{
		Task<List<Account>> GetAccountInfo();

		Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

		Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default);

		Task<List<Account>> GetAccountsAsync(string? symbolFilter, CancellationToken cancellationToken = default);

		Task<List<string>> GetSymbolProfilesAsync(int? accountFilter, CancellationToken cancellationToken = default);
	}
}
