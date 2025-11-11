using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
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
	public partial class AccountDetail : ComponentBase, IDisposable
	{
		[Parameter]
		public int AccountId { get; set; }

		[Inject]
		private IAccountDataService? AccountDataService { get; set; }

		[Inject]
		private ITransactionService? TransactionService { get; set; }

		[Inject]
		private ICurrencyExchange? CurrencyExchange { get; set; }

		[Inject]
		private IServerConfigurationService ServerConfigurationService { get; set; } = default!;

		[Inject]
		private NavigationManager NavigationManager { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		// Account information
		protected Account? Account { get; set; }
		protected string AccountName => Account?.Name ?? "Unknown Account";

		// State
		protected bool IsLoading { get; set; }
		protected bool IsTransactionsLoading { get; set; }
		protected bool HasError { get; set; }
		protected string ErrorMessage { get; set; } = string.Empty;

		// Account history data
		protected List<AccountValueHistoryPoint> AccountHistory { get; set; } = [];
		protected List<AccountValueDisplayModel> AccountHistoryDisplay { get; set; } = [];

		// View mode for value history
		protected string ValueViewMode { get; set; } = "chart";

		// Sorting state for history table
		private string sortColumn = "Date";
		private bool sortAscending = false; // Default to newest first

		// Plotly chart for account value history
		protected Config plotConfig = new();
		protected Plotly.Blazor.Layout plotLayout = new();
		protected IList<ITrace> plotData = [];

		// Current values (most recent)
		protected Money CurrentTotalValue { get; set; } = Money.Zero(Currency.EUR);
		protected Money CurrentAssetValue { get; set; } = Money.Zero(Currency.EUR);
		protected Money CurrentCashBalance { get; set; } = Money.Zero(Currency.EUR);
		protected Money CurrentInvested { get; set; } = Money.Zero(Currency.EUR);
		protected Money CurrentGainLoss { get; set; } = Money.Zero(Currency.EUR);
		protected decimal CurrentGainLossPercentage { get; set; }

		// Transactions
		protected List<TransactionDisplayModel> Transactions { get; set; } = [];
		protected Dictionary<string, int> TransactionTypeBreakdown { get; set; } = [];
		protected int totalTransactions = 0;
		protected int pageSize = 20; // Show top 20 transactions

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

		protected override async Task OnParametersSetAsync()
		{
			// Check if filter state has changed or if this is the first load
			if (AccountId > 0 && (Account == null || !FilterState.IsEqual(_previousFilterState)))
			{
				_previousFilterState = new(FilterState);
				await LoadAccountDetailAsync();
			}
		}

		private async void OnFilterStateChanged(object? sender, PropertyChangedEventArgs e)
		{
			await LoadAccountDetailAsync();
			StateHasChanged();
		}

		protected async Task LoadAccountDetailAsync()
		{
			IsLoading = true;
			HasError = false;
			ErrorMessage = string.Empty;
			StateHasChanged();
			await Task.Yield();

			try
			{
				if (AccountDataService == null || TransactionService == null)
				{
					throw new InvalidOperationException("Required services are not initialized.");
				}

				// Load account information
				var accounts = await AccountDataService.GetAccountInfo();
				Account = accounts.FirstOrDefault(a => a.Id == AccountId);

				if (Account == null)
				{
					throw new InvalidOperationException($"Account with ID {AccountId} not found.");
				}

				// Load account value history
				var allAccountHistory = await AccountDataService.GetAccountValueHistoryAsync(
					FilterState.StartDate,
					FilterState.EndDate
				) ?? [];

				// Filter to this specific account
				AccountHistory = [.. allAccountHistory.Where(h => h.AccountId == AccountId)];

				await PrepareAccountDisplayData();
				await PrepareChartData();
				CalculateCurrentValues();

				// Load transactions asynchronously
				_ = InvokeAsync(async () =>
				{
					try
					{
						await LoadTransactionsAsync();
					}
					catch (Exception ex)
					{
						// Handle transaction loading errors gracefully
						Console.WriteLine($"Error loading transactions: {ex.Message}");
						StateHasChanged();
					}
				});
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

		private async Task LoadTransactionsAsync()
		{
			IsTransactionsLoading = true;
			StateHasChanged();

			try
			{
				if (TransactionService == null)
				{
					return;
				}

				var parameters = new TransactionQueryParameters
				{
					TargetCurrency = ServerConfigurationService.PrimaryCurrency,
					StartDate = FilterState.StartDate,
					EndDate = FilterState.EndDate,
					AccountId = AccountId,
					Symbol = "",
					TransactionType = "",
					SearchText = "",
					SortColumn = "Date",
					SortAscending = false, // Newest first
					PageNumber = 1,
					PageSize = pageSize
				};

				var result = await TransactionService.GetTransactionsPaginatedAsync(parameters);
				Transactions = result.Transactions;
				TransactionTypeBreakdown = result.TransactionTypeBreakdown;
				totalTransactions = result.TotalCount;
			}
			finally
			{
				IsTransactionsLoading = false;
				StateHasChanged();
			}
		}

		private async Task PrepareAccountDisplayData()
		{
			if (AccountHistory.Count == 0)
			{
				AccountHistoryDisplay = [];
				return;
			}

			var displayData = new List<AccountValueDisplayModel>();

			foreach (var point in AccountHistory)
			{
				var gainLoss = point.TotalAssetValue.Subtract(point.TotalInvested);
				var gainLossPercentage = point.TotalInvested.Amount == 0 ? 0 : gainLoss.Amount / point.TotalInvested.Amount;

				displayData.Add(new AccountValueDisplayModel
				{
					Date = point.Date,
					AccountName = Account!.Name,
					AccountId = point.AccountId,
					Value = point.TotalValue,
					Invested = point.TotalInvested,
					Balance = point.CashBalance,
					GainLoss = gainLoss,
					GainLossPercentage = gainLossPercentage,
					Currency = ServerConfigurationService.PrimaryCurrency.Symbol
				});
			}

			AccountHistoryDisplay = displayData;
			SortDisplayData();

			await Task.CompletedTask;
		}

		private async Task PrepareChartData()
		{
			if (AccountHistory.Count == 0)
			{
				plotData = [];
				return;
			}

			await Task.Run(() =>
			{
				var sortedHistory = AccountHistory.OrderBy(h => h.Date).ToList();

				// Compute the X-axis dates once and reuse for all traces
				var xAxisDates = sortedHistory.Select(h => h.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();

				// Create traces for total value, asset value, and cash balance
				var totalValueTrace = new Scatter
				{
					X = xAxisDates,
					Y = sortedHistory.Select(h => (object)h.TotalValue.Amount).ToArray(),
					Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
					Name = "Total Value",
					Line = new Plotly.Blazor.Traces.ScatterLib.Line { Width = 3, Color = "#0d6efd" },
					Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Size = 6, Color = "#0d6efd" }
				};

				var assetValueTrace = new Scatter
				{
					X = xAxisDates,
					Y = sortedHistory.Select(h => (object)h.TotalAssetValue.Amount).ToArray(),
					Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
					Name = "Asset Value",
					Line = new Plotly.Blazor.Traces.ScatterLib.Line { Width = 2, Color = "#17a2b8" },
					Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Size = 4, Color = "#17a2b8" }
				};

				var cashBalanceTrace = new Scatter
				{
					X = xAxisDates,
					Y = sortedHistory.Select(h => (object)h.CashBalance.Amount).ToArray(),
					Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
					Name = "Cash Balance",
					Line = new Plotly.Blazor.Traces.ScatterLib.Line { Width = 2, Color = "#28a745" },
					Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Size = 4, Color = "#28a745" }
				};

				plotData = [totalValueTrace, assetValueTrace, cashBalanceTrace];

				plotLayout = new Plotly.Blazor.Layout
				{
					Title = new Plotly.Blazor.LayoutLib.Title { Text = $"{AccountName} - Value History" },
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
			});
		}

		private void CalculateCurrentValues()
		{
			if (AccountHistory.Count == 0)
			{
				var currency = ServerConfigurationService.PrimaryCurrency;
				CurrentTotalValue = Money.Zero(currency);
				CurrentAssetValue = Money.Zero(currency);
				CurrentCashBalance = Money.Zero(currency);
				CurrentInvested = Money.Zero(currency);
				CurrentGainLoss = Money.Zero(currency);
				CurrentGainLossPercentage = 0;
				return;
			}

			// Get the most recent values
			var latest = AccountHistory.OrderByDescending(h => h.Date).First();
			CurrentTotalValue = latest.TotalValue;
			CurrentAssetValue = latest.TotalAssetValue;
			CurrentCashBalance = latest.CashBalance;
			CurrentInvested = latest.TotalInvested;
			CurrentGainLoss = latest.TotalAssetValue.Subtract(latest.TotalInvested);
			CurrentGainLossPercentage = latest.TotalInvested.Amount == 0 ? 0 : CurrentGainLoss.Amount / latest.TotalInvested.Amount;
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
				sortAscending = column != "Date";
			}
			SortDisplayData();
		}

		private void SortDisplayData()
		{
			AccountHistoryDisplay = sortColumn switch
			{
				"Date" => sortAscending
					? [.. AccountHistoryDisplay.OrderBy(d => d.Date)]
					: [.. AccountHistoryDisplay.OrderByDescending(d => d.Date)],
				"TotalValue" => sortAscending
					? [.. AccountHistoryDisplay.OrderBy(d => d.Value.Amount)]
					: [.. AccountHistoryDisplay.OrderByDescending(d => d.Value.Amount)],
				"AssetValue" => sortAscending
					? [.. AccountHistoryDisplay.OrderBy(d => d.Value.Amount - d.Balance.Amount)]
					: [.. AccountHistoryDisplay.OrderByDescending(d => d.Value.Amount - d.Balance.Amount)],
				"CashBalance" => sortAscending
					? [.. AccountHistoryDisplay.OrderBy(d => d.Balance.Amount)]
					: [.. AccountHistoryDisplay.OrderByDescending(d => d.Balance.Amount)],
				"Invested" => sortAscending
					? [.. AccountHistoryDisplay.OrderBy(d => d.Invested.Amount)]
					: [.. AccountHistoryDisplay.OrderByDescending(d => d.Invested.Amount)],
				"GainLoss" => sortAscending
					? [.. AccountHistoryDisplay.OrderBy(d => d.GainLoss.Amount)]
					: [.. AccountHistoryDisplay.OrderByDescending(d => d.GainLoss.Amount)],
				"GainLossPercentage" => sortAscending
					? [.. AccountHistoryDisplay.OrderBy(d => d.GainLossPercentage)]
					: [.. AccountHistoryDisplay.OrderByDescending(d => d.GainLossPercentage)],
				_ => AccountHistoryDisplay
			};
		}

		protected void GoBackToAccounts()
		{
			NavigationManager.NavigateTo("/accounts");
		}

		protected void NavigateToHoldingDetail(string symbol)
		{
			NavigationManager.NavigateTo($"/holding-detail/{symbol}");
		}

		protected static string GetTypeClass(string type)
		{
			return type switch
			{
				"Buy" => "bg-success",
				"Sell" => "bg-danger",
				"Dividend" => "bg-info",
				"Deposit" or "CashDeposit" => "bg-primary",
				"Withdrawal" or "CashWithdrawal" => "bg-warning",
				"Fee" => "bg-dark",
				"Interest" => "bg-secondary",
				_ => "bg-light text-dark"
			};
		}

		protected static string GetValueClass(Money value, string transactionType)
		{
			// For sell transactions and income, positive values are good
			// For buy transactions and expenses, they represent outflows
			return transactionType switch
			{
				"Sell" or "Dividend" or "Interest" or "Deposit" or "CashDeposit" => value.Amount >= 0 ? "text-success" : "text-danger",
				"Buy" or "Fee" or "Withdrawal" or "CashWithdrawal" => "text-primary", // Neutral for regular transactions
				_ => value.Amount >= 0 ? "text-success" : "text-danger"
			};
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