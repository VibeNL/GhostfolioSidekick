using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.ComponentModel;
using System.Globalization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Accounts : ComponentBase, IDisposable
	{
		[Inject]
		private IAccountDataService? AccountDataService { get; set; }

		[Inject]
		private ICurrencyExchange? CurrencyExchange { get; set; }

		[Inject]
		private IServerConfigurationService ServerConfigurationService { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		// View mode for chart/table toggle
		protected string ViewMode { get; set; } = "chart";
		
		// View mode for Account Details section (table, pie, treemap)
		protected string AccountDetailsViewMode { get; set; } = "table";

		// Properties that read from cascaded filter state
		protected DateOnly StartDate => FilterState.StartDate;
		protected DateOnly EndDate => FilterState.EndDate;
		protected DateOnly MinDate { get; set; } = DateOnly.FromDayNumber(1);

		// State
		protected bool IsLoading { get; set; } = false;
		protected bool HasError { get; set; } = false;
		protected string ErrorMessage { get; set; } = string.Empty;
		protected List<AccountValueHistoryPoint> AccountsData { get; set; } = [];
		protected List<AccountValueDisplayModel> AccountDisplayData { get; set; } = [];

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending = false;

		// Plotly chart for historical data
		protected Config plotConfig = new();
		protected Plotly.Blazor.Layout plotLayout = new();
		protected IList<ITrace> plotData = [];

		// Plotly charts for account details visualization
		protected Config accountPieConfig = new();
		protected Plotly.Blazor.Layout accountPieLayout = new();
		protected IList<ITrace> accountPieData = [];
		
		protected Config accountTreemapConfig = new();
		protected Plotly.Blazor.Layout accountTreemapLayout = new();
		protected IList<ITrace> accountTreemapData = [];

		// Summary data
		protected Dictionary<string, int> AccountBreakdown { get; set; } = [];
		protected List<AccountValueDisplayModel> LatestAccountValues { get; set; } = [];

		// Financial metrics
		protected Money TotalPortfolioValue { get; set; } = Money.Zero(Currency.EUR);
		protected Money TotalAssetValue { get; set; } = Money.Zero(Currency.EUR);
		protected Money TotalCashPosition { get; set; } = Money.Zero(Currency.EUR);
		
		// Computed properties for totals in the summary table
		protected Money TotalGainLoss => LatestAccountValues.Count != 0
			? LatestAccountValues.Aggregate(Money.Zero(TotalPortfolioValue.Currency), (sum, account) => sum.Add(account.GainLoss))
			: Money.Zero(Currency.EUR);
		
		protected decimal TotalGainLossPercentage
		{
			get
			{
				if (LatestAccountValues.Count == 0) return 0m;
				var totalInvested = LatestAccountValues.Sum(x => x.Invested.Amount);
				var totalGainLoss = LatestAccountValues.Sum(x => x.GainLoss.Amount);
				return totalInvested == 0 ? 0 : totalGainLoss / totalInvested;
			}
		}

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

				AccountsData = await AccountDataService.GetAccountValueHistoryAsync(
					StartDate,
					EndDate
				) ?? [];

				await PrepareDisplayData();
				await PrepareChartData();
				PrepareSummaryData();
				await PrepareAccountDetailsCharts();
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
				AccountDisplayData = [];
				return;
			}

			var displayData = new List<AccountValueDisplayModel>();
			
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
					Currency = ServerConfigurationService.PrimaryCurrency.Symbol
				});
			}

			AccountDisplayData = displayData;
			SortDisplayData();
		}

		private async Task PrepareChartData()
		{
			if (AccountsData.Count == 0)
			{
				plotData = [];
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
				XAxis = [new Plotly.Blazor.LayoutLib.XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Date" } }],
				YAxis = [new Plotly.Blazor.LayoutLib.YAxis { Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Value ({ServerConfigurationService.PrimaryCurrency.Symbol})" } }],
				Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 60, R = 30, B = 40 },
				AutoSize = true,
				ShowLegend = true,
				Legend =
				[
					new Plotly.Blazor.LayoutLib.Legend
					{
						Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.V,
						X = 1.02m,
						Y = 1m
					}
				]
			};
			plotConfig = new Config { Responsive = true };
		}

		private async Task PrepareAccountDetailsCharts()
		{
			if (LatestAccountValues.Count == 0)
			{
				accountPieData = [];
				accountTreemapData = [];
				return;
			}

			await Task.Run(() =>
			{
				// Prepare Pie Chart
				var pieTrace = new Pie
				{
					Labels = LatestAccountValues.Select(a => a.AccountName).ToArray(),
					Values = LatestAccountValues.Select(a => (object)a.Value.Amount).ToList(),
					TextInfo = Plotly.Blazor.Traces.PieLib.TextInfoFlag.Label | Plotly.Blazor.Traces.PieLib.TextInfoFlag.Percent | Plotly.Blazor.Traces.PieLib.TextInfoFlag.Value,
					HoverTemplate = "<b>%{label}</b><br>" +
									"Value: %{customdata[0]}<br>" +
									"Gain/Loss: %{customdata[1]}<br>" +
									"Percentage: %{percent}<br>" +
									"<extra></extra>",
					CustomData = LatestAccountValues.Select(a => new object[]
					{
						CurrencyDisplay.DisplaySignAndAmount(a.Value),
						CurrencyDisplay.DisplaySignAndAmount(a.GainLoss)
					}).Cast<object>().ToList(),
					Marker = new Plotly.Blazor.Traces.PieLib.Marker
					{
						Colors = LatestAccountValues.Select(a => GetColorForGainLoss(a.GainLossPercentage)).ToList()
					}
				};

				accountPieData = [pieTrace];

				accountPieLayout = new Plotly.Blazor.Layout
				{
					Title = new Plotly.Blazor.LayoutLib.Title { Text = "Account Value Distribution" },
					Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 10, R = 10, B = 10 },
					AutoSize = true,
					ShowLegend = true,
					Legend =
					[
						new Plotly.Blazor.LayoutLib.Legend
						{
							Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.V,
							X = 1.02m,
							Y = 0.5m
						}
					]
				};

				accountPieConfig = new Config { Responsive = true };

				// Prepare Treemap
				var treemapTrace = new TreeMap
				{
					Labels = LatestAccountValues.Select(a => a.AccountName).ToArray(),
					Values = LatestAccountValues.Select(a => (object)a.Value.Amount).ToList(),
					Parents = LatestAccountValues.Select(a => "").ToArray(),
					Text = LatestAccountValues.Select(a => 
						$"{a.AccountName}<br>{CurrencyDisplay.DisplaySignAndAmount(a.Value)}<br>Gain/Loss: {CurrencyDisplay.DisplaySignAndAmount(a.GainLoss)} ({a.GainLossPercentage:P2})").ToArray(),
					TextInfo = Plotly.Blazor.Traces.TreeMapLib.TextInfoFlag.Text,
					BranchValues = Plotly.Blazor.Traces.TreeMapLib.BranchValuesEnum.Total,
					PathBar = new Plotly.Blazor.Traces.TreeMapLib.PathBar
					{
						Visible = false
					},
					Marker = new Plotly.Blazor.Traces.TreeMapLib.Marker
					{
						Colors = LatestAccountValues.Select(a => (object)GetColorForGainLoss(a.GainLossPercentage)).ToList(),
						Line = new Plotly.Blazor.Traces.TreeMapLib.MarkerLib.Line
						{
							Width = 2,
							Color = "#000000"
						}
					},
					TextFont = new Plotly.Blazor.Traces.TreeMapLib.TextFont
					{
						Size = 12,
						Color = "#000000"
					}
				};

				accountTreemapData = [treemapTrace];

				accountTreemapLayout = new Plotly.Blazor.Layout
				{
					Title = new Plotly.Blazor.LayoutLib.Title { Text = "Account Value Treemap" },
					Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 10, R = 10, B = 10 },
					AutoSize = true,
				};

				accountTreemapConfig = new Config { Responsive = true };
			});
		}

		private static object GetColorForGainLoss(decimal gainLossPercentage)
		{
			if (Math.Abs(gainLossPercentage) < 0.01m)
			{
				return "#e3f2fd"; // Light blue for neutral - more pleasing than gray
			}

			// Clamp the percentage to a reasonable range for color intensity
			const decimal maxAbs = 50m; // 50% gain/loss is max intensity
			var clamped = Math.Max(-maxAbs, Math.Min(maxAbs, gainLossPercentage));
			var intensity = (int)(Math.Min(Math.Abs(clamped) / maxAbs, 1m) * 255);

			if (clamped > 0)
			{
				// Green: from light green (#e8f5e8) to stronger green (#4caf50)
				int r = 232 - (int)(184 * (intensity / 255.0)); // fades from 232 to 76 (4c in hex)
				int g = 245 - (int)(70 * (intensity / 255.0));  // fades from 245 to 175 (af in hex)
				int b = 232 - (int)(152 * (intensity / 255.0)); // fades from 232 to 80 (50 in hex)
				return $"#{r:X2}{g:X2}{b:X2}";
			}
			else
			{
				// Red: from light red (#ffebee) to stronger red (#f44336)
				int r = 255 - (int)(11 * (intensity / 255.0));  // fades from 255 to 244 (f4 in hex)
				int g = 235 - (int)(168 * (intensity / 255.0)); // fades from 235 to 67 (43 in hex)
				int b = 238 - (int)(184 * (intensity / 255.0)); // fades from 238 to 54 (36 in hex)
				return $"#{r:X2}{g:X2}{b:X2}";
			}
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
			var currency = ServerConfigurationService.PrimaryCurrency;
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