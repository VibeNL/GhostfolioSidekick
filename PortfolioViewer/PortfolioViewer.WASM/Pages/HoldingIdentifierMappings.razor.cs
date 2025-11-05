using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
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

		protected override async Task OnInitializedAsync()
		{
			await LoadDataAsync();
		}

		protected override async Task OnParametersSetAsync()
		{
			// Reload data when the symbol parameter changes
			if (IsLoading == false) // Only reload if we've already loaded initially
			{
				await LoadDataAsync();
			}
		}

		private async Task LoadDataAsync()
		{
			try
			{
				IsLoading = true;
				HasError = false;
				StateHasChanged();

				if (!string.IsNullOrEmpty(Symbol))
				{
					// Load single holding mapping
					CurrentHoldingMapping = await HoldingIdentifierMappingService.GetHoldingIdentifierMappingAsync(Symbol);
					if (CurrentHoldingMapping != null)
					{
						HoldingMappings = [CurrentHoldingMapping];
					}
					else
					{
						HoldingMappings = [];
					}
				}
				else
				{
					// Load all holding mappings
					HoldingMappings = await HoldingIdentifierMappingService.GetAllHoldingIdentifierMappingsAsync();
					CurrentHoldingMapping = null;
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
			// Cleanup if needed
		}
	}
}