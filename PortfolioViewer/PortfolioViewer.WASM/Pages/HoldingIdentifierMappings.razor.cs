using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class HoldingIdentifierMappings : ComponentBase, IDisposable
	{
		[Parameter] public string? Symbol { get; set; }

		// State
		protected bool IsLoading { get; set; } = true;
		protected bool HasError { get; set; }
		protected string ErrorMessage { get; set; } = string.Empty;

		// Data
		protected List<HoldingIdentifierMappingModel> HoldingMappings { get; set; } = [];
		protected HoldingIdentifierMappingModel? CurrentHoldingMapping { get; set; }

		// Sorting state
		protected string sortColumn = "Symbol";
		protected bool sortAscending = true;

		// Related transactions state
		protected List<TransactionDisplayModel> RelatedTransactions { get; set; } = [];
		protected bool IsTransactionsLoading { get; set; }
		protected bool TransactionsError { get; set; }
		protected string TransactionsErrorMessage { get; set; } = string.Empty;

		protected TransactionDisplayModel? SelectedTransaction { get; set; }
		protected bool ShowTransactionModal { get; set; }

		[Inject] private Data.Services.ITransactionService TransactionService { get; set; } = default!;
		[Inject] protected Services.IHoldingIdentifierMappingService HoldingIdentifierMappingService { get; set; } = default!;
		[Inject] protected Services.ITestContextService TestContextService { get; set; } = default!;
		[Inject] protected NavigationManager Navigation { get; set; } = default!;

		protected override async Task OnInitializedAsync()
		{
			await LoadDataAsync();
		}

		protected override async Task OnParametersSetAsync()
		{
			if (!IsLoading)
			{
				await LoadDataAsync();
			}
		}

		protected async Task LoadDataAsync()
		{
			await Task.Yield();
			try
			{
				IsLoading = true;
				HasError = false;
				StateHasChanged();

				if (!string.IsNullOrEmpty(Symbol))
				{
					CurrentHoldingMapping = await HoldingIdentifierMappingService.GetHoldingIdentifierMappingAsync(Symbol);
					if (CurrentHoldingMapping != null)
					{
						HoldingMappings = [CurrentHoldingMapping];
					}
					else
					{
						HoldingMappings = [];
					}
					await LoadRelatedTransactionsAsync();
				}
				else
				{
					HoldingMappings = await HoldingIdentifierMappingService.GetAllHoldingIdentifierMappingsAsync();
					CurrentHoldingMapping = null;
					RelatedTransactions.Clear();
				}

				SortHoldingMappings();
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = $"Failed to load identifier mappings: {ex.Message}";
			}
			finally
			{
				IsLoading = false;
				StateHasChanged();
			}
		}

		protected async Task LoadRelatedTransactionsAsync()
		{
			RelatedTransactions.Clear();
			IsTransactionsLoading = true;
			TransactionsError = false;
			TransactionsErrorMessage = string.Empty;
			try
			{
				if (!string.IsNullOrEmpty(Symbol))
				{
					var parameters = new TransactionQueryParameters
					{
						Symbol = Symbol,
						StartDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-10)),
						EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
						PageNumber = 1,
						PageSize = 100,
						SortColumn = "Date",
						SortAscending = false
					};
					var result = await TransactionService.GetTransactionsPaginatedAsync(parameters);
					RelatedTransactions = result.Transactions;
				}
			}
			catch (Exception ex)
			{
				TransactionsError = true;
				TransactionsErrorMessage = ex.Message;
			}
			finally
			{
				IsTransactionsLoading = false;
				StateHasChanged();
			}
		}

		protected void SortBy(string column)
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

			SortHoldingMappings();
		}

		protected void SortHoldingMappings()
		{
			HoldingMappings = sortColumn switch
			{
				"Symbol" => sortAscending
					 ? [.. HoldingMappings.OrderBy(h => h.Symbol)]
					 : [.. HoldingMappings.OrderByDescending(h => h.Symbol)],
				"Name" => sortAscending
				 ? [.. HoldingMappings.OrderBy(h => h.Name)]
				 : [.. HoldingMappings.OrderByDescending(h => h.Name)],
				_ => HoldingMappings
			};
		}

		protected void ShowTransactionDetails(TransactionDisplayModel txn)
		{
			SelectedTransaction = txn;
			ShowTransactionModal = true;
		}

		protected void CloseTransactionModal()
		{
			ShowTransactionModal = false;
			SelectedTransaction = null;
		}

		public void Dispose()
		{
		}
	}
}