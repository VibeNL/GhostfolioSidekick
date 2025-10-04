using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Transactions : IDisposable
	{
		[Inject]
		private ITransactionService HoldingsDataService { get; set; } = default!;

		[Inject]
		private IServerConfigurationService ServerConfigurationService { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		private List<TransactionDisplayModel> TransactionsList = [];
		private PaginatedTransactionResult? currentResult;

		// Loading state management
		private bool IsLoading { get; set; } = true;
		private bool IsPageLoading { get; set; } = false;
		private bool HasError { get; set; } = false;
		private string ErrorMessage { get; set; } = string.Empty;

		// Modal state
		private TransactionDisplayModel? SelectedTransaction { get; set; }

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending = false;

		// Pagination state
		private int currentPage = 1;
		private int pageSize = 25;
		private int totalRecords = 0;
		private List<int> pageSizeOptions = [10, 25, 50, 100, 250];

		private FilterState? _previousFilterState;

		protected override Task OnInitializedAsync()
		{
			// Subscribe to filter changes
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}
			
			return Task.CompletedTask;
		}

		protected override Task OnParametersSetAsync()
		{
			// Check if filter state has changed
			if (FilterState.IsEqual(_previousFilterState))
			{
				return Task.CompletedTask;
			}

			_previousFilterState = new(FilterState);
			return LoadTransactionDataAsync();
		}

		private async void OnFilterStateChanged(object? sender, PropertyChangedEventArgs e)
		{
			Console.WriteLine($"Transactions OnFilterStateChanged - Property: {e.PropertyName}");

			await InvokeAsync(async () =>
			{
				currentPage = 1; // Reset to first page when filters change
				await LoadTransactionDataAsync();
				StateHasChanged();
			});
		}

		private async Task LoadTransactionDataAsync()
		{
			try
			{
				IsLoading = true;
				HasError = false;
				ErrorMessage = string.Empty;
				StateHasChanged(); // Update UI to show loading state

				// Yield control to allow UI to update
				await Task.Yield();

				currentResult = await LoadPaginatedTransactionDataAsync();
				TransactionsList = currentResult?.Transactions ?? [];
				totalRecords = currentResult?.TotalCount ?? 0;
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = ex.Message;
			}
			finally
			{
				IsLoading = false;
				StateHasChanged(); // Update UI when loading is complete
			}
		}

		private async Task LoadPageDataAsync()
		{
			try
			{
				IsPageLoading = true;
				HasError = false;
				ErrorMessage = string.Empty;
				StateHasChanged(); // Update UI to show page loading state

				// Yield control to allow UI to update
				await Task.Yield();

				currentResult = await LoadPaginatedTransactionDataAsync();
				TransactionsList = currentResult?.Transactions ?? [];
				totalRecords = currentResult?.TotalCount ?? 0;
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = ex.Message;
			}
			finally
			{
				IsPageLoading = false;
				StateHasChanged(); // Update UI when loading is complete
			}
		}

		private async Task<PaginatedTransactionResult> LoadPaginatedTransactionDataAsync()
		{
			try
			{
				var result = await (HoldingsDataService?.GetTransactionsPaginatedAsync(
					ServerConfigurationService.PrimaryCurrency,
					FilterState.StartDate,
					FilterState.EndDate,
					FilterState.SelectedAccountId,
					FilterState.SelectedSymbol ?? "",
					FilterState.SelectedTransactionType ?? "",
					FilterState.SearchText ?? "",
					sortColumn,
					sortAscending,
					currentPage,
					pageSize) ?? Task.FromResult(new PaginatedTransactionResult()));

				return result;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to load transaction data: {ex.Message}", ex);
			}
		}

		// Modal methods
		private void ShowTransactionDetails(TransactionDisplayModel transaction)
		{
			SelectedTransaction = transaction;
			StateHasChanged();
		}

		// Pagination methods
		private async Task OnPageChanged(int newPage)
		{
			if (newPage != currentPage)
			{
				currentPage = newPage;
				await LoadPageDataAsync();
			}
		}

		private async Task OnPageSizeChanged(int newPageSize)
		{
			if (newPageSize != pageSize)
			{
				pageSize = newPageSize;
				currentPage = 1; // Reset to first page when changing page size
				await LoadTransactionDataAsync(); // Reload with new page size
			}
		}

		// Statistics based on all filtered transactions (not just current page)
		private Dictionary<string, int> TransactionTypeBreakdown =>
			currentResult?.TransactionTypeBreakdown ?? [];

		private Dictionary<string, int> AccountBreakdown =>
			currentResult?.AccountBreakdown ?? [];

		private List<string> AvailableTransactionTypes =>
			TransactionTypeBreakdown?.Keys
				   .Where(t => !string.IsNullOrEmpty(t))
				   .OrderBy(t => t)
				   .ToList() ?? [];

		private async Task SortBy(string column)
		{
			if (sortColumn == column)
			{
				sortAscending = !sortAscending;
			}
			else
			{
				sortColumn = column;
				sortAscending = true;
			}
			
			currentPage = 1; // Reset to first page when sorting
			await LoadTransactionDataAsync();
		}

		private static string GetTypeClass(string type)
		{
			return type switch
			{
				"Buy" => "bg-success",
				"Sell" => "bg-danger",
				"Dividend" => "bg-info",
				"Deposit" or "CashDeposit" => "bg-success",
				"Withdrawal" or "CashWithdrawal" => "bg-warning",
				"Fee" => "bg-danger",
				"Interest" => "bg-info",
				"Receive" => "bg-success",
				"Send" => "bg-warning",
				"Staking Reward" or "StakingReward" => "bg-primary",
				"Gift Fiat" or "GiftFiat" => "bg-secondary",
				"Gift Asset" or "GiftAsset" => "bg-secondary",
				"Valuable" => "bg-secondary",
				"Liability" => "bg-secondary",
				"Repay Bond" or "RepayBond" => "bg-secondary",
				_ => "bg-secondary"
			};
		}

		private static string GetValueClass(Money? value, string type)
		{
			if (value == null) return "";

			return type switch
			{
				"Buy" => "text-danger",
				"Sell" => "text-success",
				"Dividend" => "text-success",
				"Deposit" or "CashDeposit" => "text-success",
				"Withdrawal" or "CashWithdrawal" => "text-danger",
				"Fee" => "text-danger",
				"Interest" => "text-success",
				"Receive" => "text-success",
				"Send" => "text-danger",
				"Staking Reward" or "StakingReward" => "text-success",
				"Gift Fiat" or "GiftFiat" => "text-success",
				"Gift Asset" or "GiftAsset" => "text-success",
				"Valuable" => "text-success",
				"Liability" => "text-danger",
				"Repay Bond" or "RepayBond" => "text-success",
				_ => ""
			};
		}

		public void Dispose()
		{
			if (FilterState != null)
			{
				FilterState.PropertyChanged -= OnFilterStateChanged;
			}
			if (_previousFilterState != null)
			{
				_previousFilterState.PropertyChanged -= OnFilterStateChanged;
			}
		}
	}
}