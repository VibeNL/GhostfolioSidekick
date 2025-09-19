using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.Globalization;
using System.Collections.Generic;
using GhostfolioSidekick.Database.Repository;
using System.ComponentModel;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Accounts : ComponentBase, IDisposable
	{
		[Inject]
		private IAccountDataService? AccountDataService { get; set; }

		[Inject]
		private ICurrencyExchange? CurrencyExchange { get; set; }

		[Inject]
		private ISyncConfigurationService? SyncConfigurationService { get; set; }

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		// View mode for chart/table toggle
		protected string ViewMode { get; set; } = "chart";

		// Properties that read from cascaded filter state
		protected DateOnly StartDate => FilterState.StartDate;
		protected DateOnly EndDate => FilterState.EndDate;
		protected string SelectedCurrency => SyncConfigurationService?.TargetCurrency.Symbol ?? "EUR";

		protected DateOnly MinDate { get; set; } = DateOnly.FromDayNumber(1);

		// State
		protected bool IsLoading { get; set; } = false;
		protected bool HasError { get; set; } = false;
		protected string ErrorMessage { get; set; } = string.Empty;
		protected List<AccountValueHistoryPoint> AccountsData { get; set; } = new();
		protected List<AccountValueDisplayModel> AccountDisplayData { get; set; } = new();

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending = false;

		// Plotly chart
		protected Config plotConfig = new();
		protected Plotly.Blazor.Layout plotLayout = new();
		protected IList<ITrace> plotData = new List<ITrace>();

		// Summary data
		protected Dictionary<string, int> AccountBreakdown { get; set; } = new();
		protected List<AccountValueDisplayModel> LatestAccountValues { get; set; } = new();

		// Financial metrics
		protected Money TotalPortfolioValue { get; set; } = Money.Zero(Currency.EUR);
		protected Money TotalAssetValue { get; set; } = Money.Zero(Currency.EUR);
		protected Money TotalCashPosition { get; set; } = Money.Zero(Currency.EUR);

		private FilterState? _previousFilterState;

		protected override async Task OnInitializedAsync()
		{
			if (AccountDataService != null)
			{
				MinDate = await AccountDataService.GetMinDateAsync();
			}

			// Subscribe to filter changes
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}
		}

		protected override async Task OnParametersSetAsync()
		{
			// Check if filter state has changed
			if (FilterState.IsEqual(_previousFilterState))
			{
				return;
			}

			_previousFilterState = new(FilterState);
			await LoadAccountDataAsync();
		}

		private async void OnFilterStateChanged(object? sender, PropertyChangedEventArgs e)
		{
			await LoadAccountDataAsync();
			StateHasChanged();
		}

		protected async Task LoadAccountDataAsync()
		{
			IsLoading = true;
			HasError = false;
			ErrorMessage = string.Empty;
			StateHasChanged();
			await Task.Yield();

			try
			{
				if (AccountDataService == null || CurrencyExchange == null)
				{
					throw new InvalidOperationException("HoldingsDataService or CurrencyExchange is not initialized.");
				}

				var currency = Currency.GetCurrency(SelectedCurrency);
				AccountsData = await AccountDataService.GetAccountValueHistoryAsync(
					currency,
					StartDate,
					EndDate
				) ?? new List<AccountValueHistoryPoint>();

				await PrepareDisplayData();
				await PrepareChartData();
				PrepareSummaryData();
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

		private async Task PrepareDisplayData()
		{
			if (AccountsData.Count == 0)
			{
				AccountDisplayData = new List<AccountValueDisplayModel>();
				return;
			}

			var displayData = new List<AccountValueDisplayModel>();
			var targetCurrency = Currency.GetCurrency(SelectedCurrency);

			var accounts = (await AccountDataService!.GetAccountInfo()).ToDictionary(x => x.Id, x => x);

			foreach (var point in AccountsData)
			{
				var gainLoss = point.TotalAssetValue.Subtract(point.TotalInvested);
				var gainLossPercentage = point.TotalInvested.Amount == 0 ? 0 : gainLoss.Amount / point.TotalInvested.Amount;

				displayData.Add(new AccountValueDisplayModel
				{
					Date = point.Date,
					AccountName = accounts[point.AccountId].Name,
					AccountId = point.AccountId,
					Value = point.TotalValue,
					Invested = point.TotalInvested,
					Balance = point.CashBalance,
					GainLoss = gainLoss,
					GainLossPercentage = gainLossPercentage,
					Currency = targetCurrency.Symbol.ToString()
				});
			}

			AccountDisplayData = displayData;
			SortDisplayData();
		}

		private async Task PrepareChartData()
		{
			if (AccountsData.Count == 0)
			{
				plotData = new List<ITrace>();
				return;
			}

			var accounts = (await AccountDataService!.GetAccountInfo()).ToDictionary(x => x.Id, x => x);
			var accountGroups = AccountsData.GroupBy(a => a.AccountId).ToList();
			var traces = new List<ITrace>();

			// Create a line for each account
			foreach (var accountGroup in accountGroups)
			{
				var accountData = accountGroup.OrderBy(a => a.Date).ToList();

				var trace = new Scatter
				{
					X = accountData.Select(a => a.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray(),
					Y = accountData.Select(a => (object)a.TotalAssetValue.Amount).ToArray(),
					Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
					Name = accounts[accountGroup.Key].Name,
					Line = new Plotly.Blazor.Traces.ScatterLib.Line { Width = 2 },
					Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Size = 6 }
				};

				traces.Add(trace);
			}

			plotData = traces;
			plotLayout = new Plotly.Blazor.Layout
			{
				Title = new Plotly.Blazor.LayoutLib.Title { Text = "Account Values Over Time" },
				XAxis = new List<Plotly.Blazor.LayoutLib.XAxis> { new Plotly.Blazor.LayoutLib.XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Date" } } },
				YAxis = new List<Plotly.Blazor.LayoutLib.YAxis> { new Plotly.Blazor.LayoutLib.YAxis { Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Value ({SelectedCurrency})" } } },
				Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 60, R = 30, B = 40 },
				AutoSize = true,
				ShowLegend = true,
				Legend = new List<Plotly.Blazor.LayoutLib.Legend>
				{
					new Plotly.Blazor.LayoutLib.Legend
					{
						Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.V,
						X = 1.02m,
						Y = 1m
					}
				}
			};
			plotConfig = new Config { Responsive = true };
		}

		private void PrepareSummaryData()
		{
			// Account breakdown
			AccountBreakdown = AccountDisplayData
				.GroupBy(a => a.AccountName)
				.ToDictionary(g => g.Key, g => g.Count());

			// Latest account values (most recent date for each account)
			LatestAccountValues = AccountDisplayData
				.GroupBy(a => a.AccountName)
				.Select(g => g.OrderByDescending(a => a.Date).First())
				.OrderByDescending(a => a.Value.Amount)
				.ToList();

			// Calculate financial metrics from latest values
			var currency = Currency.GetCurrency(SelectedCurrency);
			TotalPortfolioValue = LatestAccountValues.Aggregate(Money.Zero(currency), (sum, account) => sum.Add(account.Value));
			TotalCashPosition = LatestAccountValues.Aggregate(Money.Zero(currency), (sum, account) => sum.Add(account.Balance));
			TotalAssetValue = TotalPortfolioValue.Subtract(TotalCashPosition);
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
			SortDisplayData();
		}

		private void SortDisplayData()
		{
			switch (sortColumn)
			{
				case "Date":
					AccountDisplayData = sortAscending
						? AccountDisplayData.OrderBy(d => d.Date).ToList()
						: AccountDisplayData.OrderByDescending(d => d.Date).ToList();
					break;
				case "Account":
					AccountDisplayData = sortAscending
						? AccountDisplayData.OrderBy(d => d.AccountName).ToList()
						: AccountDisplayData.OrderByDescending(d => d.AccountName).ToList();
					break;
				case "Value":
					AccountDisplayData = sortAscending
						? AccountDisplayData.OrderBy(d => d.Value.Amount).ToList()
						: AccountDisplayData.OrderByDescending(d => d.Value.Amount).ToList();
					break;
				case "Invested":
					AccountDisplayData = sortAscending
						? AccountDisplayData.OrderBy(d => d.Invested.Amount).ToList()
						: AccountDisplayData.OrderByDescending(d => d.Invested.Amount).ToList();
					break;
				case "Balance":
					AccountDisplayData = sortAscending
						? AccountDisplayData.OrderBy(d => d.Balance.Amount).ToList()
						: AccountDisplayData.OrderByDescending(d => d.Balance.Amount).ToList();
					break;
				case "GainLoss":
					AccountDisplayData = sortAscending
						? AccountDisplayData.OrderBy(d => d.GainLoss.Amount).ToList()
						: AccountDisplayData.OrderByDescending(d => d.GainLoss.Amount).ToList();
					break;
				case "GainLossPercentage":
					AccountDisplayData = sortAscending
						? AccountDisplayData.OrderBy(d => d.GainLossPercentage).ToList()
						: AccountDisplayData.OrderByDescending(d => d.GainLossPercentage).ToList();
					break;
				default:
					break;
			}
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