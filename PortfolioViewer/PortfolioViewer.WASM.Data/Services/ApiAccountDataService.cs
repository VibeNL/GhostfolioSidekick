using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// An <see cref="IAccountDataService"/> implementation that queries the API directly.
	/// </summary>
	public class ApiAccountDataService(HttpClient httpClient, IServerConfigurationService serverConfigurationService) : ApiServiceBase, IAccountDataService
	{
		public async Task<List<Account>> GetAccountInfo()
		{
			var dtos = await httpClient.GetFromJsonAsync<List<AccountDto>>("api/accounts", JsonOptions);
			return dtos?.Select(MapAccount).ToList() ?? [];
		}

		public async Task<Account?> GetAccountByIdAsync(int accountId)
		{
			var response = await httpClient.GetAsync($"api/accounts/{accountId}");
			if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return null;
			}

			response.EnsureSuccessStatusCode();
			var dto = await response.Content.ReadFromJsonAsync<AccountDto>(JsonOptions);
			return dto == null ? null : MapAccount(dto);
		}

		public async Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
		{
			var url = $"api/accounts/value-history?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
			var dtos = await httpClient.GetFromJsonAsync<List<AccountValueHistoryPointDto>>(url, JsonOptions, cancellationToken);
			if (dtos == null)
			{
				return null;
			}

			var currency = serverConfigurationService.PrimaryCurrency;
			return dtos.Select(d => new AccountValueHistoryPoint
			{
				Date = d.Date,
				AccountId = d.AccountId,
				TotalAssetValue = new Money(currency, d.TotalAssetValue),
				TotalInvested = new Money(currency, d.TotalInvested),
				CashBalance = new Money(currency, d.CashBalance),
				TotalValue = new Money(currency, d.TotalValue),
			}).ToList();
		}

		public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
		{
			var result = await httpClient.GetFromJsonAsync<string>("api/accounts/min-date", JsonOptions, cancellationToken);
			return result != null ? DateOnly.Parse(result) : DateOnly.FromDateTime(DateTime.Today);
		}

		public async Task<List<Account>> GetAccountsAsync(string? symbolFilter, CancellationToken cancellationToken = default)
		{
			var url = string.IsNullOrWhiteSpace(symbolFilter)
				? "api/accounts"
				: $"api/accounts?symbolFilter={Uri.EscapeDataString(symbolFilter)}";
			var dtos = await httpClient.GetFromJsonAsync<List<AccountDto>>(url, JsonOptions, cancellationToken);
			return dtos?.Select(MapAccount).ToList() ?? [];
		}

		public async Task<List<string>> GetSymbolProfilesAsync(int? accountFilter, CancellationToken cancellationToken = default)
		{
			var url = accountFilter.HasValue
				? $"api/accounts/symbol-profiles?accountFilter={accountFilter}"
				: "api/accounts/symbol-profiles";
			var result = await httpClient.GetFromJsonAsync<List<string>>(url, JsonOptions, cancellationToken);
			return result ?? [];
		}

		public async Task<List<TaxReportRow>> GetTaxReportAsync(CancellationToken cancellationToken = default)
		{
			var dtos = await httpClient.GetFromJsonAsync<List<TaxReportRowDto>>("api/accounts/tax-report", JsonOptions, cancellationToken);
			if (dtos == null)
			{
				return [];
			}

			var currency = serverConfigurationService.PrimaryCurrency;
			return dtos.Select(d => new TaxReportRow
			{
				Year = d.Year,
				Date = d.Date,
				AccountId = d.AccountId,
				AccountName = d.AccountName,
				AssetValue = new Money(currency, d.AssetValue),
				CashBalance = new Money(currency, d.CashBalance),
				TotalValue = new Money(currency, d.TotalValue),
			}).ToList();
		}

		private static Account MapAccount(AccountDto dto) => new(dto.Name)
		{
			Id = dto.Id,
			Comment = dto.Comment,
			SyncActivities = dto.SyncActivities,
			SyncBalance = dto.SyncBalance,
			Platform = dto.PlatformName != null ? new Platform(dto.PlatformName) : null,
		};

		private sealed class AccountDto
		{
			public int Id { get; set; }
			public string Name { get; set; } = string.Empty;
			public string? Comment { get; set; }
			public bool SyncActivities { get; set; }
			public bool SyncBalance { get; set; }
			public string? PlatformName { get; set; }
		}

		private sealed class AccountValueHistoryPointDto
		{
			public DateOnly Date { get; set; }
			public int AccountId { get; set; }
			public decimal TotalAssetValue { get; set; }
			public decimal TotalInvested { get; set; }
			public decimal CashBalance { get; set; }
			public decimal TotalValue { get; set; }
		}

		private sealed class TaxReportRowDto
		{
			public int Year { get; set; }
			public DateOnly Date { get; set; }
			public int AccountId { get; set; }
			public string AccountName { get; set; } = string.Empty;
			public decimal AssetValue { get; set; }
			public decimal CashBalance { get; set; }
			public decimal TotalValue { get; set; }
		}
	}
}
