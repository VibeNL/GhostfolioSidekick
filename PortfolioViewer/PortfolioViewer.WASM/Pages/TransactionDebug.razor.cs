using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class TransactionDebug : ComponentBase
	{
		private bool IsLoading = true;
		private bool HasError = false;
		private string? ErrorMessage;
		private List<TransactionDebugRow> TransactionDebugRows = [];
		private List<AccountInfo> Accounts = [];
		private int? SelectedAccountId;
		
		// Filtering fields for symbol type
		protected string SelectedAssetClass = string.Empty;
		protected string SelectedAssetSubClass = string.Empty;
		protected List<string> AssetClassOptions = ["", "Liquidity", "Commodity", "Equity", "FixedIncome", "RealEstate"];
		protected List<string> AssetSubClassOptions = ["", "CryptoCurrency", "Etf", "Stock", "MutualFund", "Bond", "Commodity", "PreciousMetal", "PrivateEquity"];
     protected string SelectedSymbol = string.Empty;
		protected IEnumerable<string> GetSymbolOptions()
		{
			return TransactionDebugRows
				.Select(r => r.Symbol)
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Distinct()
				.OrderBy(s => s);
		}
		protected bool HasAssetClassData => TransactionDebugRows.Any(row => !string.IsNullOrWhiteSpace(row.AssetClass));
		protected bool HasAssetSubClassData => TransactionDebugRows.Any(row => !string.IsNullOrWhiteSpace(row.AssetSubClass));
		protected IEnumerable<TransactionDebugRow> FilteredRows =>
			TransactionDebugRows.Where(row =>
				(string.IsNullOrEmpty(SelectedAssetClass) || !HasAssetClassData || row.AssetClass == SelectedAssetClass)
				&& (string.IsNullOrEmpty(SelectedAssetSubClass) || !HasAssetSubClassData || row.AssetSubClass == SelectedAssetSubClass)
				&& (string.IsNullOrEmpty(SelectedSymbol) || row.Symbol == SelectedSymbol)
			);

		[Inject]
		private ITransactionService TransactionService { get; set; } = null!;

		[Inject]
		private IAccountDataService AccountDataService { get; set; } = null!;

		protected override async Task OnInitializedAsync()
		{
			try
			{
				IsLoading = true;
				HasError = false;
				Accounts = [.. (await AccountDataService.GetAccountInfo()).Select(a => new AccountInfo { Id = a.Id, Name = a.Name })];
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = ex.Message;
			}
			finally
			{
				IsLoading = false;
			}
		}

		private async Task OnAccountChanged(ChangeEventArgs e)
		{
			IsLoading = true;
			StateHasChanged();

			await Task.Delay(100); // Allow UI to update

			if (int.TryParse(e.Value?.ToString(), out var id))
			{
				SelectedAccountId = id;
				await LoadForSelectedAccount();
			}
			else
			{
				SelectedAccountId = null;
				TransactionDebugRows = [];
			}

			IsLoading = false;
			StateHasChanged();
		}

		private async Task LoadForSelectedAccount()
		{
			if (SelectedAccountId == null || SelectedAccountId == 0)
			{
				TransactionDebugRows = [];
				StateHasChanged();
				return;
			}
			try
			{
				HasError = false;
				TransactionDebugRows = await LoadTransactionDebugRowsAsync(SelectedAccountId.Value);
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = ex.Message;
			}
		}

		private async Task<List<TransactionDebugRow>> LoadTransactionDebugRowsAsync(int accountId)
		{
			var accounts = await AccountDataService.GetAccountInfo();
			var parameters = new Data.Models.TransactionQueryParameters
			{
				StartDate = DateOnly.MinValue,
				EndDate = DateOnly.MaxValue,
				AccountId = accountId,
				Symbol = string.Empty,
				TransactionTypes = [],
				SearchText = string.Empty,
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = int.MaxValue
			};
			var paged = await TransactionService.GetTransactionsPaginatedAsync(parameters);
			var transactions = paged.Transactions.OrderBy(t => t.Date).ToList();
			if (!transactions.Any())
				return new List<TransactionDebugRow>();

			var minDate = DateOnly.FromDateTime(transactions.First().Date);
			var maxDate = DateOnly.FromDateTime(transactions.Last().Date);

			var valueHistory = await AccountDataService.GetAccountValueHistoryAsync(minDate, maxDate);
			var valueHistoryForAccount = valueHistory?.Where(x => x.AccountId == accountId).ToList() ?? new List<Data.Models.AccountValueHistoryPoint>();

			var debugRows = new List<TransactionDebugRow>();
			foreach (var t in transactions)
			{
				var account = accounts.FirstOrDefault(a => a.Name == t.AccountName);
				if (account == null) continue;

				var txDateOnly = DateOnly.FromDateTime(t.Date);
				var historyPoint = valueHistoryForAccount
					.Where(h => h.Date <= txDateOnly)
					.OrderByDescending(h => h.Date)
					.FirstOrDefault();

				string cashBalanceDisplay = historyPoint != null
					? $"{historyPoint.CashBalance.Currency.Symbol} {historyPoint.CashBalance.Amount:N2}"
					: "N/A";
				string assetValueDisplay = historyPoint != null
					? $"{historyPoint.TotalAssetValue.Currency.Symbol} {historyPoint.TotalAssetValue.Amount:N2}"
					: "N/A";

				var assetStateDisplay = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(t.Symbol) && historyPoint != null)
				{
					assetStateDisplay[t.Symbol] = assetValueDisplay;
				}

				debugRows.Add(new TransactionDebugRow
				{
					Date = t.Date,
					Type = t.Type,
					Symbol = t.Symbol ?? string.Empty,
					AccountName = t.AccountName,
					CashBalanceDisplay = cashBalanceDisplay,
					AssetStates = assetStateDisplay,
					TransactionId = t.TransactionId
				});
			}
			return debugRows;
		}

       public class TransactionDebugRow
	   {
		   public DateTime Date { get; set; }
		   public string Type { get; set; } = string.Empty;
		   public string Symbol { get; set; } = string.Empty;
		   public string AccountName { get; set; } = string.Empty;
		   public string CashBalanceDisplay { get; set; } = string.Empty;
		   public Dictionary<string, string> AssetStates { get; set; } = [];
		   public string TransactionId { get; set; } = string.Empty;
		   public string? AssetClass { get; set; }
		   public string? AssetSubClass { get; set; }
	   }

		public class AccountInfo
		{
			public int Id { get; set; }
			public string Name { get; set; } = string.Empty;
		}
	}
}
