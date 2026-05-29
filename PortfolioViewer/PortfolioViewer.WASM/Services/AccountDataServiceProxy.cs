using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Delegates to either the local-DB or API-backed <see cref="IAccountDataService"/> based on
	/// <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class AccountDataServiceProxy(
		[FromKeyedServices(DataSourceKeys.Local)] IAccountDataService localService,
		[FromKeyedServices(DataSourceKeys.Api)] IAccountDataService apiService,
		IDataSourceService dataSourceService) : IAccountDataService
	{
		private IAccountDataService Active =>
			dataSourceService.UseApiDirectly ? apiService : localService;

		public Task<List<Account>> GetAccountInfo()
			=> Active.GetAccountInfo();

		public Task<Account?> GetAccountByIdAsync(int accountId)
			=> Active.GetAccountByIdAsync(accountId);

		public Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
			=> Active.GetAccountValueHistoryAsync(startDate, endDate, cancellationToken);

		public Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
			=> Active.GetMinDateAsync(cancellationToken);

		public Task<List<Account>> GetAccountsAsync(string? symbolFilter, CancellationToken cancellationToken = default)
			=> Active.GetAccountsAsync(symbolFilter, cancellationToken);

		public Task<List<string>> GetSymbolProfilesAsync(int? accountFilter, CancellationToken cancellationToken = default)
			=> Active.GetSymbolProfilesAsync(accountFilter, cancellationToken);

		public Task<List<TaxReportRow>> GetTaxReportAsync(CancellationToken cancellationToken = default)
			=> Active.GetTaxReportAsync(cancellationToken);
	}
}
