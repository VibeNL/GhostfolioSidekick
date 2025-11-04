using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class TaskStatus : ComponentBase
	{
		[Inject] private DatabaseContext DbContext { get; set; } = default!;

		private List<TaskRun>? _taskRuns;
		private bool _isLoading = true;
		private string _sortColumn = nameof(TaskRun.InProgress);
		private string _sortDirection = "desc"; // Start with InProgress tasks at top
		private TaskRun? _selectedTaskError;

		// Statistics
		private int _runningTasks;
		private int _scheduledTasks;
		private int _errorTasks;

		protected override async Task OnInitializedAsync()
		{
			await LoadTaskData();
		}

		private async Task LoadTaskData()
		{
			try
			{
				_isLoading = true;
				StateHasChanged();

				_taskRuns = await DbContext.Tasks
					.AsNoTracking()
					.ToListAsync();

				CalculateStatistics();
			}
			catch (Exception ex)
			{
				// Handle error gracefully
				Console.WriteLine($"Error loading task data: {ex.Message}");
				_taskRuns = new List<TaskRun>();
			}
			finally
			{
				_isLoading = false;
				StateHasChanged();
			}
		}

		private void CalculateStatistics()
		{
			if (_taskRuns == null) return;

			_runningTasks = _taskRuns.Count(t => t.InProgress);
			_scheduledTasks = _taskRuns.Count(t => t.Scheduled && !t.InProgress);
			_errorTasks = _taskRuns.Count(t => !string.IsNullOrEmpty(t.LastException));
		}

		private async Task RefreshData()
		{
			await LoadTaskData();
		}

		private void SortBy(string columnName)
		{
			if (_sortColumn == columnName)
			{
				// Toggle sort direction if clicking the same column
				_sortDirection = _sortDirection == "asc" ? "desc" : "asc";
			}
			else
			{
				// Set new sort column and default to ascending
				_sortColumn = columnName;
				_sortDirection = "asc";
			}

			StateHasChanged();
		}

		private IEnumerable<TaskRun> GetSortedTasks()
		{
			if (_taskRuns == null) return Enumerable.Empty<TaskRun>();

			// Apply primary sorting and then secondary sorts
			return _sortColumn switch
			{
				nameof(TaskRun.InProgress) => _sortDirection == "asc" 
					? _taskRuns.OrderBy(t => t.InProgress).ThenBy(t => t.Priority).ThenBy(t => t.NextSchedule)
					: _taskRuns.OrderByDescending(t => t.InProgress).ThenBy(t => t.Priority).ThenBy(t => t.NextSchedule),
				nameof(TaskRun.Priority) => _sortDirection == "asc"
					? _taskRuns.OrderBy(t => t.Priority).ThenByDescending(t => t.InProgress).ThenBy(t => t.NextSchedule)
					: _taskRuns.OrderByDescending(t => t.Priority).ThenByDescending(t => t.InProgress).ThenBy(t => t.NextSchedule),
				nameof(TaskRun.Name) => _sortDirection == "asc"
					? _taskRuns.OrderBy(t => t.Name).ThenByDescending(t => t.InProgress).ThenBy(t => t.Priority)
					: _taskRuns.OrderByDescending(t => t.Name).ThenByDescending(t => t.InProgress).ThenBy(t => t.Priority),
				nameof(TaskRun.NextSchedule) => _sortDirection == "asc"
					? _taskRuns.OrderBy(t => t.NextSchedule).ThenByDescending(t => t.InProgress).ThenBy(t => t.Priority)
					: _taskRuns.OrderByDescending(t => t.NextSchedule).ThenByDescending(t => t.InProgress).ThenBy(t => t.Priority),
				_ => _taskRuns.OrderByDescending(t => t.InProgress).ThenBy(t => t.Priority).ThenBy(t => t.NextSchedule)
			};
		}

		private string GetRowClass(TaskRun task)
		{
			if (task.InProgress)
				return "row-running";
			if (!string.IsNullOrEmpty(task.LastException))
				return "row-error";
			if (task.NextSchedule != DateTimeOffset.MaxValue && task.NextSchedule < DateTimeOffset.Now)
				return "row-overdue";
			return string.Empty;
		}

		private string GetNextScheduleClass(DateTimeOffset nextSchedule)
		{
			if (nextSchedule < DateTimeOffset.Now)
				return "text-warning fw-bold";
			if (nextSchedule < DateTimeOffset.Now.AddHours(1))
				return "text-info";
			return string.Empty;
		}

		private string GetRelativeTime(DateTimeOffset dateTime)
		{
			if (dateTime == DateTimeOffset.MinValue || dateTime == DateTimeOffset.MaxValue)
				return string.Empty;

			var timeDiff = DateTimeOffset.Now - dateTime;
			var isInFuture = timeDiff < TimeSpan.Zero;
			timeDiff = timeDiff.Duration(); // Get absolute value

			var prefix = isInFuture ? "in " : "";
			var suffix = isInFuture ? "" : " ago";

			if (timeDiff.TotalMinutes < 1)
				return isInFuture ? "in a few seconds" : "a few seconds ago";
			if (timeDiff.TotalMinutes < 60)
				return $"{prefix}{(int)timeDiff.TotalMinutes} min{suffix}";
			if (timeDiff.TotalHours < 24)
				return $"{prefix}{(int)timeDiff.TotalHours} hr{suffix}";
			if (timeDiff.TotalDays < 7)
				return $"{prefix}{(int)timeDiff.TotalDays} day{(timeDiff.TotalDays >= 2 ? "s" : "")}{suffix}";
			if (timeDiff.TotalDays < 30)
				return $"{prefix}{(int)(timeDiff.TotalDays / 7)} week{(timeDiff.TotalDays >= 14 ? "s" : "")}{suffix}";
			return $"{prefix}{(int)(timeDiff.TotalDays / 30)} month{(timeDiff.TotalDays >= 60 ? "s" : "")}{suffix}";
		}

		private void ShowErrorDetails(TaskRun task)
		{
			_selectedTaskError = task;
			StateHasChanged();
		}

		private void CloseErrorDetails()
		{
			_selectedTaskError = null;
			StateHasChanged();
		}
	}
}