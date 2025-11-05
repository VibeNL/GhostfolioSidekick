using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class HoldingIdentifierMappings : IDisposable
	{
		[Parameter] public string? Symbol { get; set; }

		// State
		private bool IsLoading { get; set; } = true;
		private bool HasError { get; set; }
		private string ErrorMessage { get; set; } = string.Empty;

		// Data
		private List<HoldingIdentifierMappingModel> HoldingMappings = [];
		private HoldingIdentifierMappingModel? CurrentHoldingMapping { get; set; }

		// Sorting state
		private string sortColumn = "Symbol";
		private bool sortAscending = true;

		// Related transactions state
		private List<GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TransactionDisplayModel> RelatedTransactions = new();
		private bool IsTransactionsLoading = false;
		private bool TransactionsError = false;
		private string TransactionsErrorMessage = string.Empty;

		[Inject] private GhostfolioSidekick.PortfolioViewer.WASM.Data.Services.ITransactionService TransactionService { get; set; } = default!;

		protected override async Task OnInitializedAsync()
		{
			await LoadDataAsync();
		}

		protected override async Task OnParametersSetAsync()
		{
			if (IsLoading == false)
			{
				await LoadDataAsync();
			}
		}

		private async Task LoadDataAsync()
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

		private async Task LoadRelatedTransactionsAsync()
		{
			RelatedTransactions.Clear();
			IsTransactionsLoading = true;
			TransactionsError = false;
			TransactionsErrorMessage = string.Empty;
			try
			{
				if (!string.IsNullOrEmpty(Symbol))
				{
					var parameters = new GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TransactionQueryParameters
					{
						Symbol = Symbol,
						StartDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-10)),
						EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
						PageNumber =1,
						PageSize =100,
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

			SortHoldingMappings();
		}

		private void SortHoldingMappings()
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

		public void Dispose()
		{
		}
	}
}