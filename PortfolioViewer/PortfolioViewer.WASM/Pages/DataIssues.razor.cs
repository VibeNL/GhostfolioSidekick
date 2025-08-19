using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class DataIssues : IDisposable
	{
		[Inject]
		private IDataIssuesService? DataIssuesService { get; set; }

		[Inject]
		private NavigationManager? Navigation { get; set; }

		// State
		private bool IsLoading { get; set; } = true;
		private bool HasError { get; set; } = false;
		private string ErrorMessage { get; set; } = string.Empty;

		// Data
		private List<DataIssueDisplayModel> DataIssuesList = new();
		private List<DataIssueDisplayModel> FilteredDataIssuesList = new();

		// Filters
		private string SelectedSeverity = string.Empty;
		private string SelectedActivityType = string.Empty;
		private string SelectedAccount = string.Empty;

		// Filter options
		private List<string> ActivityTypes = new();
		private List<string> Accounts = new();

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending = false;

		// Summary data
		private Dictionary<string, int> SeverityBreakdown = new();
		private Dictionary<string, int> ActivityTypeBreakdown = new();
		private Dictionary<string, int> AccountBreakdown = new();

		protected override async Task OnInitializedAsync()
		{
			await LoadDataIssuesAsync();
		}

		private async Task LoadDataIssuesAsync()
		{
			try
			{
				IsLoading = true;
				HasError = false;
				StateHasChanged();

				DataIssuesList = await DataIssuesService!.GetActivitiesWithoutHoldingsAsync();
				
				// Extract filter options
				ActivityTypes = DataIssuesList.Select(d => d.ActivityType).Distinct().OrderBy(x => x).ToList();
				Accounts = DataIssuesList.Select(d => d.AccountName).Distinct().OrderBy(x => x).ToList();

				// Calculate breakdowns
				SeverityBreakdown = DataIssuesList.GroupBy(d => d.Severity).ToDictionary(g => g.Key, g => g.Count());
				ActivityTypeBreakdown = DataIssuesList.GroupBy(d => d.ActivityType).ToDictionary(g => g.Key, g => g.Count());
				AccountBreakdown = DataIssuesList.GroupBy(d => d.AccountName).ToDictionary(g => g.Key, g => g.Count());

				// Initial filter
				FilterDataIssues();
				SortDataIssues();
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = $"Failed to load data issues: {ex.Message}";
			}
			finally
			{
				IsLoading = false;
				StateHasChanged();
			}
		}

		private async Task RefreshDataAsync()
		{
			await LoadDataIssuesAsync();
		}

		private void FilterDataIssues()
		{
			FilteredDataIssuesList = DataIssuesList.Where(issue =>
				(string.IsNullOrEmpty(SelectedSeverity) || issue.Severity == SelectedSeverity) &&
				(string.IsNullOrEmpty(SelectedActivityType) || issue.ActivityType == SelectedActivityType) &&
				(string.IsNullOrEmpty(SelectedAccount) || issue.AccountName == SelectedAccount)
			).ToList();

			SortDataIssues();
			StateHasChanged();
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

			SortDataIssues();
		}

		private void SortDataIssues()
		{
			FilteredDataIssuesList = sortColumn switch
			{
				"Date" => sortAscending 
					? FilteredDataIssuesList.OrderBy(d => d.Date).ToList()
					: FilteredDataIssuesList.OrderByDescending(d => d.Date).ToList(),
				"Severity" => sortAscending 
					? FilteredDataIssuesList.OrderBy(d => GetSeverityOrder(d.Severity)).ToList()
					: FilteredDataIssuesList.OrderByDescending(d => GetSeverityOrder(d.Severity)).ToList(),
				"ActivityType" => sortAscending 
					? FilteredDataIssuesList.OrderBy(d => d.ActivityType).ToList()
					: FilteredDataIssuesList.OrderByDescending(d => d.ActivityType).ToList(),
				"AccountName" => sortAscending 
					? FilteredDataIssuesList.OrderBy(d => d.AccountName).ToList()
					: FilteredDataIssuesList.OrderByDescending(d => d.AccountName).ToList(),
				_ => FilteredDataIssuesList
			};
		}

		private static string GetSeverityClass(string severity)
		{
			return severity switch
			{
				"Error" => "bg-danger",
				"Warning" => "bg-warning text-dark",
				"Info" => "bg-info text-dark",
				_ => "bg-secondary"
			};
		}

		private static string GetSeverityIcon(string severity)
		{
			return severity switch
			{
				"Error" => "bi bi-exclamation-triangle-fill",
				"Warning" => "bi bi-exclamation-circle-fill",
				"Info" => "bi bi-info-circle-fill",
				_ => "bi bi-question-circle-fill"
			};
		}

		private static int GetSeverityOrder(string severity)
		{
			return severity switch
			{
				"Error" => 1,
				"Warning" => 2,
				"Info" => 3,
				_ => 4
			};
		}

		private static string GetActivityTypeClass(string activityType)
		{
			return activityType switch
			{
				"Buy" => "bg-success",
				"Sell" => "bg-danger",
				"Dividend" => "bg-primary",
				"Deposit" => "bg-info text-dark",
				"Withdrawal" => "bg-warning text-dark",
				"Fee" => "bg-dark",
				"Interest" => "bg-secondary",
				_ => "bg-light text-dark"
			};
		}

		public void Dispose()
		{
			// Cleanup if needed
		}
	}
}