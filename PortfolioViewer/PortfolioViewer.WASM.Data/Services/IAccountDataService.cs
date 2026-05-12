using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface IAccountDataService
	{
		Task<List<Account>> GetAccountInfo();

		Task<Account?> GetAccountByIdAsync(int accountId);

       Task<IEnumerable<GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TransactionRow>> GetTransactionsForAccountAsync(int accountId, int? year);

		Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

		Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default);

		Task<List<Account>> GetAccountsAsync(string? symbolFilter, CancellationToken cancellationToken = default);

		Task<List<string>> GetSymbolProfilesAsync(int? accountFilter, CancellationToken cancellationToken = default);

		Task<List<TaxReportRow>> GetTaxReportAsync(CancellationToken cancellationToken = default);
	}
}
