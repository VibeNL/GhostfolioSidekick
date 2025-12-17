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
				Accounts = (await AccountDataService.GetAccountInfo())
					.Select(a => new AccountInfo { Id = a.Id, Name = a.Name })
					.ToList();
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

			var cashBalances = accounts.ToDictionary(a => a.Id, a => 0m);
			var assetStates = accounts.ToDictionary(a => a.Id, a => new Dictionary<string, decimal>());

			var debugRows = new List<TransactionDebugRow>();
			foreach (var t in transactions)
			{
				var account = accounts.FirstOrDefault(a => a.Name == t.AccountName);
				if (account == null) continue;
				var accId = account.Id;

				decimal cashDelta = 0;
				if (t.Type == "Buy" && t.TotalValue != null)
					cashDelta = -t.TotalValue.Amount;
				else if (t.Type == "Sell" && t.TotalValue != null)
					cashDelta = t.TotalValue.Amount;
				else if ((t.Type == "Deposit" || t.Type == "CashDeposit") && t.Amount != null)
					cashDelta = t.Amount.Amount;
				else if ((t.Type == "Withdrawal" || t.Type == "CashWithdrawal") && t.Amount != null)
					cashDelta = -t.Amount.Amount;
				else if (t.Type == "Dividend" && t.Amount != null)
					cashDelta = t.Amount.Amount;
				else if (t.Type == "Fee" && t.Amount != null)
					cashDelta = -t.Amount.Amount;
				else if (t.Type == "Interest" && t.Amount != null)
					cashDelta = t.Amount.Amount;

				cashBalances[accId] += cashDelta;

				var assets = assetStates[accId];
				if (!string.IsNullOrEmpty(t.Symbol))
				{
					if (!assets.ContainsKey(t.Symbol))
						assets[t.Symbol] = 0m;
					if (t.Type == "Buy" && t.Quantity.HasValue)
						assets[t.Symbol] += t.Quantity.Value;
					else if (t.Type == "Sell" && t.Quantity.HasValue)
						assets[t.Symbol] -= t.Quantity.Value;
				}

				var assetStateDisplay = assets
					.Where(kvp => kvp.Value != 0m)
					.ToDictionary(
						kvp => kvp.Key,
						kvp => kvp.Value.ToString("N4")
					);

				debugRows.Add(new TransactionDebugRow
				{
					Date = t.Date,
					Type = t.Type,
					Symbol = t.Symbol ?? string.Empty,
					AccountName = t.AccountName,
					CashBalanceDisplay = t.TotalValue != null ? t.TotalValue.Currency.Symbol + " " + cashBalances[accId].ToString("N2") : cashBalances[accId].ToString("N2"),
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
		}

		public class AccountInfo
		{
			public int Id { get; set; }
			public string Name { get; set; } = string.Empty;
		}
	}
}
