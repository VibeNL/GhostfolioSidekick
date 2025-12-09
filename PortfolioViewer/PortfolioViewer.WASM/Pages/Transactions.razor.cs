using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using System.Web;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Transactions : IDisposable
	{
		[Inject]
		private ITransactionService HoldingsDataService { get; set; } = default!;

		[Inject]
		private IServerConfigurationService ServerConfigurationService { get; set; } = default!;

		[Inject]
		private NavigationManager Navigation { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		private List<TransactionDisplayModel> TransactionsList = [];
		private PaginatedTransactionResult? currentResult;

		// Loading state management
		private bool IsLoading { get; set; } = true;
		private bool IsPageLoading { get; set; }
		private bool HasError { get; set; }
		private string ErrorMessage { get; set; } = string.Empty;

		// Modal state
		private TransactionDisplayModel? SelectedTransaction { get; set; }

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending;

		// Pagination state
		private int currentPage = 1;
		private int pageSize = 25;
		private int totalRecords;
		private readonly List<int> pageSizeOptions = [10, 25, 50, 100, 250];

		private FilterState? _previousFilterState;
		private bool _hasAppliedUrlParameters = false;

		protected override Task OnInitializedAsync()
		{
			// Subscribe to filter changes
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}

			// Apply URL parameters on initial load
			ApplyUrlParameters();

			return Task.CompletedTask;
		}

		protected override Task OnParametersSetAsync()
		{
			// Apply URL parameters only once
			if (!_hasAppliedUrlParameters)
			{
				ApplyUrlParameters();
			}

			// Check if filter state has changed
			if (FilterState.IsEqual(_previousFilterState))
			{
				return Task.CompletedTask;
			}

			_previousFilterState = new(FilterState);
			return LoadTransactionDataAsync();
		}

		private void ApplyUrlParameters()
		{
			if (_hasAppliedUrlParameters || FilterState == null) return;

			try
			{
				var uri = new Uri(Navigation.Uri);
				var queryString = uri.Query;

				if (string.IsNullOrEmpty(queryString))
				{
					_hasAppliedUrlParameters = true;
					return;
				}

				var queryParams = System.Web.HttpUtility.ParseQueryString(queryString);

				// Check for accountId parameter
				var accountIdParam = queryParams["accountId"];
				if (!string.IsNullOrEmpty(accountIdParam) && int.TryParse(accountIdParam, out var accountId) && accountId > 0)
				{
					FilterState.SelectedAccountId = accountId;
				}

				// Check for symbol parameter
				var symbolParam = queryParams["symbol"];
				if (!string.IsNullOrEmpty(symbolParam))
				{
					FilterState.SelectedSymbol = symbolParam;
				}

				// Check for type parameter
				var typeParam = queryParams["type"];
				if (!string.IsNullOrEmpty(typeParam))
				{
					FilterState.SelectedTransactionType = [typeParam];
				}

				// Check for search parameter
				var searchParam = queryParams["search"];
				if (!string.IsNullOrEmpty(searchParam))
				{
					FilterState.SearchText = searchParam;
				}

				_hasAppliedUrlParameters = true;
			}
			catch (Exception ex)
			{
				// Log error but don't fail the page load
				Console.WriteLine($"Error applying URL parameters: {ex.Message}");
				_hasAppliedUrlParameters = true;
			}
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
				var parameters = new TransactionQueryParameters
				{
					TargetCurrency = ServerConfigurationService.PrimaryCurrency,
					StartDate = FilterState.StartDate,
					EndDate = FilterState.EndDate,
					AccountId = FilterState.SelectedAccountId,
					Symbol = FilterState.SelectedSymbol ?? "",
					TransactionTypes = FilterState.SelectedTransactionType ?? [],
					SearchText = FilterState.SearchText ?? "",
					SortColumn = sortColumn,
					SortAscending = sortAscending,
					PageNumber = currentPage,
					PageSize = pageSize
				};

				var result = await (HoldingsDataService?.GetTransactionsPaginatedAsync(parameters) ?? Task.FromResult(new PaginatedTransactionResult()));

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
			var bgsecondary = "bg-secondary";
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
				"Gift Fiat" or "GiftFiat" => bgsecondary,
				"Gift Asset" or "GiftAsset" => bgsecondary,
				"Valuable" => bgsecondary,
				"Liability" => bgsecondary,
				"Repay Bond" or "RepayBond" => bgsecondary,
				_ => bgsecondary
			};
		}

		private static string GetValueClass(Money? value, string type)
		{
			if (value == null) return "";

			const string dangerText = "text-danger";
			const string succesText = "text-success";
			return type switch
			{
				"Buy" => dangerText,
				"Sell" => succesText,
				"Dividend" => succesText,
				"Deposit" or "CashDeposit" => succesText,
				"Withdrawal" or "CashWithdrawal" => dangerText,
				"Fee" => dangerText,
				"Interest" => succesText,
				"Receive" => succesText,
				"Send" => dangerText,
				"Staking Reward" or "StakingReward" => succesText,
				"Gift Fiat" or "GiftFiat" => succesText,
				"Gift Asset" or "GiftAsset" => succesText,
				"Valuable" => succesText,
				"Liability" => dangerText,
				"Repay Bond" or "RepayBond" => succesText,
				_ => ""
			};
		}

		private void NavigateToHoldingDetail(string symbol)
		{
			if (string.IsNullOrEmpty(symbol))
				return;

			Navigation?.NavigateTo($"/holding/{Uri.EscapeDataString(symbol)}");
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