using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Transactions : IDisposable
	{
		[Inject]
		private IHoldingsDataServiceOLD? HoldingsDataService { get; set; }

		[Inject]
		private IServerConfigurationService ServerConfigurationService { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();
		
		private List<TransactionDisplayModel> TransactionsList = new();

		// Loading state management
		private bool IsLoading { get; set; } = true;
		private bool HasError { get; set; } = false;
		private string ErrorMessage { get; set; } = string.Empty;

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending = false;

		private FilterState? _previousFilterState;

		protected override async Task OnInitializedAsync()
		{
			// Subscribe to filter changes
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}
			
			await LoadTransactionDataAsync();
		}

		protected override async Task OnParametersSetAsync()
		{
			// Check if filter state has changed
			if (_previousFilterState != null &&
				   _previousFilterState.StartDate == FilterState.StartDate &&
				   _previousFilterState.EndDate == FilterState.EndDate &&
				   _previousFilterState.SelectedAccountId == FilterState.SelectedAccountId &&
				   _previousFilterState.SelectedSymbol == FilterState.SelectedSymbol)
			{
				return;
			}
			
			// Unsubscribe from old filter state
			if (_previousFilterState != null)
			{
				_previousFilterState.PropertyChanged -= OnFilterStateChanged;
			}
			
			// Subscribe to new filter state
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}
			
			_previousFilterState = FilterState;
			await LoadTransactionDataAsync();
		}

		private bool HasFilterStateChanged()
		{
			if (_previousFilterState == null) return true;
			
			return _previousFilterState.StartDate != FilterState.StartDate ||
				   _previousFilterState.EndDate != FilterState.EndDate ||
				   _previousFilterState.SelectedAccountId != FilterState.SelectedAccountId ||
				   _previousFilterState.SelectedSymbol != FilterState.SelectedSymbol;
		}

		private async void OnFilterStateChanged(object? sender, PropertyChangedEventArgs e)
		{
			Console.WriteLine($"Transactions OnFilterStateChanged - Property: {e.PropertyName}");
			
			await InvokeAsync(async () =>
			{
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

				TransactionsList = await LoadRealTransactionDataAsync();
				SortTransactions(); // Sort after loading
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

		private async Task<List<TransactionDisplayModel>> LoadRealTransactionDataAsync()
		{
			try
			{
				return await HoldingsDataService?.GetTransactionsAsync(
					ServerConfigurationService.PrimaryCurrency,
					FilterState.StartDate,
					FilterState.EndDate,
					FilterState.SelectedAccountId,
					FilterState.SelectedSymbol) ?? new List<TransactionDisplayModel>();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to load transaction data: {ex.Message}", ex);
			}
		}

		// Add refresh method for manual data reload
		private async Task RefreshDataAsync()
		{
			await LoadTransactionDataAsync();
		}

		private Dictionary<string, int> TransactionTypeBreakdown =>
			TransactionsList.GroupBy(t => t.Type)
				   .ToDictionary(g => g.Key, g => g.Count());

		private Dictionary<string, int> AccountBreakdown =>
			TransactionsList.GroupBy(t => t.AccountName)
				   .ToDictionary(g => g.Key, g => g.Count());

		private void SortBy(string column)
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
			SortTransactions();
		}

		private void SortTransactions()
		{
			switch (sortColumn)
			{
				case "Date":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Date).ToList() : TransactionsList.OrderByDescending(t => t.Date).ToList();
					break;
				case "Type":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Type).ToList() : TransactionsList.OrderByDescending(t => t.Type).ToList();
					break;
				case "Symbol":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Symbol).ToList() : TransactionsList.OrderByDescending(t => t.Symbol).ToList();
					break;
				case "Name":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Name).ToList() : TransactionsList.OrderByDescending(t => t.Name).ToList();
					break;
				case "AccountName":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.AccountName).ToList() : TransactionsList.OrderByDescending(t => t.AccountName).ToList();
					break;
				case "Quantity":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Quantity ?? 0).ToList() : TransactionsList.OrderByDescending(t => t.Quantity ?? 0).ToList();
					break;
				case "UnitPrice":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.UnitPrice?.Amount ?? 0).ToList() : TransactionsList.OrderByDescending(t => t.UnitPrice?.Amount ?? 0).ToList();
					break;
				case "TotalValue":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.TotalValue?.Amount ?? 0).ToList() : TransactionsList.OrderByDescending(t => t.TotalValue?.Amount ?? 0).ToList();
					break;
				case "Fee":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Fee?.Amount ?? 0).ToList() : TransactionsList.OrderByDescending(t => t.Fee?.Amount ?? 0).ToList();
					break;
				case "Tax":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Tax?.Amount ?? 0).ToList() : TransactionsList.OrderByDescending(t => t.Tax?.Amount ?? 0).ToList();
					break;
				case "Description":
					TransactionsList = sortAscending ? TransactionsList.OrderBy(t => t.Description).ToList() : TransactionsList.OrderByDescending(t => t.Description).ToList();
					break;
				default:
					break;
			}
		}

		private static string GetTypeClass(string type)
		{
			return type switch
			{
				"Buy" => "bg-success",
				"Sell" => "bg-danger",
				"Dividend" => "bg-info",
				"Deposit" => "bg-success",
				"Withdrawal" => "bg-warning",
				"Fee" => "bg-danger",
				"Interest" => "bg-info",
				"Receive" => "bg-success",
				"Send" => "bg-warning",
				"Staking Reward" => "bg-primary",
				"Gift" => "bg-secondary",
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
				"Deposit" => "text-success",
				"Withdrawal" => "text-danger",
				"Fee" => "text-danger",
				"Interest" => "text-success",
				"Receive" => "text-success",
				"Send" => "text-danger",
				"Staking Reward" => "text-success",
				"Gift" => "text-success",
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