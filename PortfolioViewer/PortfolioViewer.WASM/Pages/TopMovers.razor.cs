using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class TopMovers : ComponentBase, IDisposable
	{
		[Inject]
		private IHoldingsDataService HoldingsDataService { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		protected DateOnly StartDate => FilterState.StartDate;
		protected DateOnly EndDate => FilterState.EndDate;
		protected int SelectedAccountId => FilterState.SelectedAccountId;

		protected List<HoldingDisplayModel> HoldingsData { get; set; } = [];
		protected List<HoldingTimeRangePerformance> TopRisers { get; set; } = [];
		protected List<HoldingTimeRangePerformance> TopLosers { get; set; } = [];

		protected bool IsLoading { get; set; }
		protected bool HasError { get; set; }
		protected string ErrorMessage { get; set; } = string.Empty;

		private FilterState? _previousFilterState;

		protected override async Task OnInitializedAsync()
		{
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}
		}

		protected override async Task OnParametersSetAsync()
		{
			if (_previousFilterState == null || !FilterState.IsEqual(_previousFilterState))
			{
				if (_previousFilterState != null)
				{
					_previousFilterState.PropertyChanged -= OnFilterStateChanged;
				}

				FilterState.PropertyChanged += OnFilterStateChanged;
				_previousFilterState = new(FilterState);
				await LoadMoversAsync();
			}
		}

		private async void OnFilterStateChanged(object? sender, PropertyChangedEventArgs e)
		{
			await InvokeAsync(async () =>
			{
				await LoadMoversAsync();
				StateHasChanged();
			});
		}

		protected async Task LoadMoversAsync()
		{
			IsLoading = true;
			HasError = false;
			ErrorMessage = string.Empty;
			StateHasChanged();
			await Task.Yield();
			try
			{
				if (HoldingsDataService == null)
				{
					throw new InvalidOperationException("HoldingsDataService is not initialized.");
				}
				if (SelectedAccountId == 0)
				{
					HoldingsData = await HoldingsDataService.GetHoldingsAsync() ?? [];
				}
				else
				{
					HoldingsData = await HoldingsDataService.GetHoldingsAsync(SelectedAccountId) ?? [];
				}
				await PrepareRisersAndLosers();
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = ex.Message;
			}
			finally
			{
				IsLoading = false;
				StateHasChanged();
			}
		}

		private async Task PrepareRisersAndLosers()
		{
			var holdings = HoldingsData.Where(h => h.Quantity > 0 && h.CurrentValue.Amount > 0).ToList();

			// Collect all symbols and fetch price history in a single bulk query
			var symbols = holdings
				.Select(h => h.Symbols.FirstOrDefault() ?? string.Empty)
				.Where(s => !string.IsNullOrEmpty(s))
				.Distinct()
				.ToList();

			var priceHistoryBySymbol = await HoldingsDataService.GetHoldingPriceHistoryBulkAsync(
				symbols, StartDate, EndDate);

			var timeRangePerformances = new List<HoldingTimeRangePerformance>();
			foreach (var holding in holdings)
			{
				try
				{
					var symbol = holding.Symbols.FirstOrDefault() ?? string.Empty;
					if (string.IsNullOrEmpty(symbol)) continue;

					if (!priceHistoryBySymbol.TryGetValue(symbol, out var priceHistory) || priceHistory.Count == 0)
						continue;

					var startPricePoint = priceHistory.OrderBy(p => p.Date).First();
					var endPricePoint = priceHistory.OrderByDescending(p => p.Date).First();
					if (startPricePoint.Price <= 0) continue;

					var currency = holding.CurrentPrice.Currency;
					var percentageChange = (endPricePoint.Price - startPricePoint.Price) / startPricePoint.Price;
					timeRangePerformances.Add(new HoldingTimeRangePerformance
					{
						Symbol = symbol,
						Name = holding.Name,
						StartPrice = new Money(currency, startPricePoint.Price),
						EndPrice = new Money(currency, endPricePoint.Price),
						PercentageChange = percentageChange,
						AbsoluteChange = new Money(currency, endPricePoint.Price - startPricePoint.Price),
						CurrentValue = holding.CurrentValue,
						Quantity = holding.Quantity
					});
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error calculating performance for {holding.Symbols.FirstOrDefault() ?? string.Empty}: {ex.Message}");
				}
			}

			TopRisers = [.. timeRangePerformances
				.Where(h => h.PercentageChange > 0)
				.OrderByDescending(h => h.PercentageChange)
				.Take(3)];
			TopLosers = [.. timeRangePerformances
				.Where(h => h.PercentageChange < 0)
				.OrderBy(h => h.PercentageChange)
				.Take(3)];
		}

		public void Dispose()
		{
			if (FilterState != null)
			{
				FilterState.PropertyChanged -= OnFilterStateChanged;
			}
		}
	}
}
