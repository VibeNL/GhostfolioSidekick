using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class PortfolioPerformanceService
	{
		private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
		private readonly ILogger<PortfolioPerformanceService> _logger;

		public PortfolioPerformanceService(
			IDbContextFactory<DatabaseContext> dbContextFactory,
			ILogger<PortfolioPerformanceService> logger)
		{
			_dbContextFactory = dbContextFactory;
			_logger = logger;
		}

		/// <summary>
		/// Calculate all meaningful time periods based on portfolio activity data
		/// </summary>
		public async Task<List<TimePeriodInfo>> CalculateAllMeaningfulTimePeriodsAsync()
		{
			try
			{
				_logger.LogInformation("Calculating meaningful time periods from portfolio data");

				using var context = await _dbContextFactory.CreateDbContextAsync();
				
				// Get all activities to determine meaningful periods
				var allActivities = await context.Activities
					.Select(a => new { a.Date })
					.ToListAsync();

				if (!allActivities.Any())
				{
					_logger.LogWarning("No activities found for period calculation");
					return new List<TimePeriodInfo>();
				}

				var now = DateTime.Now;
				var firstActivity = allActivities.Min(a => a.Date);
				var lastActivity = allActivities.Max(a => a.Date);

				_logger.LogInformation("Portfolio activity span: {FirstActivity:yyyy-MM-dd} to {LastActivity:yyyy-MM-dd}", 
					firstActivity, lastActivity);

				var periods = new List<TimePeriodInfo>();

				// Add different types of periods
				periods.AddRange(GetStandardPeriods(now, allActivities.Select(a => a.Date).ToList()));
				periods.AddRange(GetYearlyPeriods(firstActivity, now, allActivities.Select(a => a.Date).ToList()));
				periods.AddRange(GetQuarterlyPeriods(firstActivity, now, allActivities.Select(a => a.Date).ToList()));
				periods.AddRange(GetMonthlyPeriods(now, allActivities.Select(a => a.Date).ToList()));
				periods.AddRange(GetMilestonePeriods(firstActivity, now));
				periods.AddRange(GetInceptionPeriods(firstActivity, now));
				periods.AddRange(GetRollingPeriods(firstActivity, now, allActivities.Select(a => a.Date).ToList()));

				// Remove duplicates and sort by end date descending (most recent first)
				var uniquePeriods = periods
					.GroupBy(p => new { p.StartDate, p.EndDate })
					.Select(g => g.First())
					.OrderByDescending(p => p.EndDate)
					.ThenByDescending(p => p.StartDate)
					.ToList();

				_logger.LogInformation("Calculated {TotalPeriods} meaningful time periods from portfolio activities", 
					uniquePeriods.Count);

				return uniquePeriods;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calculating meaningful time periods");
				throw;
			}
		}

		/// <summary>
		/// Generate a detailed time periods report
		/// </summary>
		public async Task<string> GenerateTimePeriodsReportAsync()
		{
			try
			{
				var allPeriods = await CalculateAllMeaningfulTimePeriodsAsync();
				var report = new System.Text.StringBuilder();

				report.AppendLine("=== Dynamic Time Periods Analysis Report ===");
				report.AppendLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				report.AppendLine("");

				if (!allPeriods.Any())
				{
					report.AppendLine("No meaningful time periods found.");
					return report.ToString();
				}

				using var context = await _dbContextFactory.CreateDbContextAsync();
				var allActivities = await context.Activities.Select(a => a.Date).ToListAsync();
				var firstActivity = allActivities.Min();
				var lastActivity = allActivities.Max();

				report.AppendLine($"Portfolio Activity Span: {firstActivity:yyyy-MM-dd} to {lastActivity:yyyy-MM-dd}");
				report.AppendLine($"Total Activity Period: {(lastActivity - firstActivity).TotalDays:F0} days");
				report.AppendLine($"Total Activities: {allActivities.Count}");
				report.AppendLine("");

				// Group and display by category
				var groupedPeriods = allPeriods
					.GroupBy(p => GetPeriodCategory(p.Name))
					.OrderBy(g => GetCategoryOrder(g.Key));

				foreach (var group in groupedPeriods)
				{
					report.AppendLine($"=== {group.Key} ===");
					
					var sortedPeriods = group.OrderByDescending(p => p.EndDate).ToList();
					
					foreach (var period in sortedPeriods)
					{
						var activitiesInPeriod = allActivities.Count(a => a >= period.StartDate && a <= period.EndDate);
						
						report.AppendLine($"  {period.Name}:");
						report.AppendLine($"    Period: {period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}");
						report.AppendLine($"    Duration: {period.DurationDays:F0} days");
						report.AppendLine($"    Activities: {activitiesInPeriod}");
					}
					report.AppendLine("");
				}

				// Summary statistics
				report.AppendLine("=== Summary Statistics ===");
				report.AppendLine($"Total Periods Calculated: {allPeriods.Count}");
				
				var periodsByCategory = groupedPeriods.ToDictionary(g => g.Key, g => g.Count());
				foreach (var (category, count) in periodsByCategory.OrderByDescending(kvp => kvp.Value))
				{
					report.AppendLine($"  {category}: {count} periods");
				}

				var avgDuration = allPeriods.Average(p => p.DurationDays);
				var longestPeriod = allPeriods.OrderByDescending(p => p.DurationDays).First();
				var shortestPeriod = allPeriods.OrderBy(p => p.DurationDays).First();

				report.AppendLine("");
				report.AppendLine($"Average Period Duration: {avgDuration:F0} days");
				report.AppendLine($"Longest Period: {longestPeriod.Name} ({longestPeriod.DurationDays:F0} days)");
				report.AppendLine($"Shortest Period: {shortestPeriod.Name} ({shortestPeriod.DurationDays:F0} days)");

				return report.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating time periods report");
				throw;
			}
		}

		/// <summary>
		/// Get portfolio statistics for analysis
		/// </summary>
		public async Task<PortfolioStatistics> GetPortfolioStatisticsAsync()
		{
			try
			{
				using var context = await _dbContextFactory.CreateDbContextAsync();

				var totalActivities = await context.Activities.CountAsync();
				var totalHoldings = await context.Holdings.CountAsync();
				var totalAccounts = await context.Accounts.CountAsync();
				var uniqueSymbols = await context.SymbolProfiles.CountAsync();

				var firstActivity = await context.Activities
					.OrderBy(a => a.Date)
					.Select(a => a.Date)
					.FirstOrDefaultAsync();

				var lastActivity = await context.Activities
					.OrderByDescending(a => a.Date)
					.Select(a => a.Date)
					.FirstOrDefaultAsync();

				// Get activity types by using the discriminator column or class name
				var activitiesByType = await context.Activities
					.ToListAsync();

				var activityTypeGroups = activitiesByType
					.GroupBy(a => a.GetType().Name)
					.ToDictionary(g => g.Key, g => g.Count());

				return new PortfolioStatistics
				{
					TotalActivities = totalActivities,
					TotalHoldings = totalHoldings,
					TotalAccounts = totalAccounts,
					UniqueSymbols = uniqueSymbols,
					FirstActivityDate = firstActivity,
					LastActivityDate = lastActivity,
					PortfolioAgeDays = firstActivity != default && lastActivity != default 
						? (lastActivity - firstActivity).TotalDays 
						: 0,
					ActivitiesByType = activityTypeGroups
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting portfolio statistics");
				throw;
			}
		}

		#region Private Helper Methods

		private List<TimePeriodInfo> GetStandardPeriods(DateTime now, List<DateTime> activityDates)
		{
			var periods = new List<TimePeriodInfo>();
			var standardPeriods = new List<(string Name, DateTime Start, DateTime End)>
			{
				("Last Week", now.AddDays(-7), now),
				("Last Month", now.AddMonths(-1), now),
				("Last Quarter", now.AddMonths(-3), now),
				("Last 6 Months", now.AddMonths(-6), now),
				("Last Year", now.AddYears(-1), now),
				("Year to Date", new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local), now),
				("Last 2 Years", now.AddYears(-2), now),
				("Last 3 Years", now.AddYears(-3), now)
			};

			foreach (var period in standardPeriods)
			{
				if (HasActivitiesInPeriod(activityDates, period.Start, period.End))
				{
					periods.Add(new TimePeriodInfo
					{
						Name = period.Name,
						StartDate = period.Start,
						EndDate = period.End,
						DurationDays = (period.End - period.Start).TotalDays
					});
				}
			}

			return periods;
		}

		private List<TimePeriodInfo> GetYearlyPeriods(DateTime firstActivity, DateTime now, List<DateTime> activityDates)
		{
			var periods = new List<TimePeriodInfo>();
			var firstYear = firstActivity.Year;
			var currentYear = now.Year;

			for (int year = firstYear; year <= currentYear; year++)
			{
				var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local);
				var yearEnd = year == currentYear ? now : new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Local);

				if (HasActivitiesInPeriod(activityDates, yearStart, yearEnd))
				{
					periods.Add(new TimePeriodInfo
					{
						Name = $"Year {year}",
						StartDate = yearStart,
						EndDate = yearEnd,
						DurationDays = (yearEnd - yearStart).TotalDays
					});
				}
			}

			return periods;
		}

		private List<TimePeriodInfo> GetQuarterlyPeriods(DateTime firstActivity, DateTime now, List<DateTime> activityDates)
		{
			var periods = new List<TimePeriodInfo>();
			var firstYear = firstActivity.Year;
			var currentYear = now.Year;

			for (int year = firstYear; year <= currentYear; year++)
			{
				for (int quarter = 1; quarter <= 4; quarter++)
				{
					var quarterStart = new DateTime(year, (quarter - 1) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Local);
					var quarterEnd = quarter == 4 && year == currentYear 
						? now 
						: quarterStart.AddMonths(3).AddDays(-1);

					// Don't add future quarters
					if (quarterStart > now) break;

					if (HasActivitiesInPeriod(activityDates, quarterStart, quarterEnd))
					{
						periods.Add(new TimePeriodInfo
						{
							Name = $"Q{quarter} {year}",
							StartDate = quarterStart,
							EndDate = quarterEnd,
							DurationDays = (quarterEnd - quarterStart).TotalDays
						});
					}
				}
			}

			return periods;
		}

		private List<TimePeriodInfo> GetMonthlyPeriods(DateTime now, List<DateTime> activityDates)
		{
			var periods = new List<TimePeriodInfo>();
			var monthStart = now.AddMonths(-24);

			while (monthStart < now)
			{
				var monthEnd = monthStart.AddMonths(1).AddDays(-1);
				if (monthEnd > now) monthEnd = now;

				if (HasActivitiesInPeriod(activityDates, monthStart, monthEnd))
				{
					periods.Add(new TimePeriodInfo
					{
						Name = $"{monthStart:MMMM yyyy}",
						StartDate = monthStart,
						EndDate = monthEnd,
						DurationDays = (monthEnd - monthStart).TotalDays
					});
				}

				monthStart = monthStart.AddMonths(1);
			}

			return periods;
		}

		private List<TimePeriodInfo> GetMilestonePeriods(DateTime firstActivity, DateTime now)
		{
			var periods = new List<TimePeriodInfo>();
			var milestoneStart = firstActivity;
			var milestoneCounter = 1;

			while (milestoneStart.AddMonths(6) <= now)
			{
				var milestoneEnd = milestoneStart.AddMonths(6);
				if (milestoneEnd > now) milestoneEnd = now;

				periods.Add(new TimePeriodInfo
				{
					Name = $"First {milestoneCounter * 6} Months",
					StartDate = milestoneStart,
					EndDate = milestoneEnd,
					DurationDays = (milestoneEnd - milestoneStart).TotalDays
				});
				milestoneStart = milestoneStart.AddMonths(6);
				milestoneCounter++;
			}

			return periods;
		}

		private List<TimePeriodInfo> GetInceptionPeriods(DateTime firstActivity, DateTime now)
		{
			var periods = new List<TimePeriodInfo>();
			
			if (firstActivity < now)
			{
				periods.Add(new TimePeriodInfo
				{
					Name = "Inception to Date",
					StartDate = firstActivity,
					EndDate = now,
					DurationDays = (now - firstActivity).TotalDays
				});
			}

			return periods;
		}

		private List<TimePeriodInfo> GetRollingPeriods(DateTime firstActivity, DateTime now, List<DateTime> activityDates)
		{
			var periods = new List<TimePeriodInfo>();
			var portfolioAge = (now - firstActivity).TotalDays;
			
			if (portfolioAge <= 365) return periods; // Only for portfolios older than 1 year

			// Add rolling 1-year periods every 3 months
			var rollingStart = firstActivity;
			while (rollingStart.AddYears(1) <= now)
			{
				var rollingEnd = rollingStart.AddYears(1);
				if (HasActivitiesInPeriod(activityDates, rollingStart, rollingEnd))
				{
					periods.Add(new TimePeriodInfo
					{
						Name = $"Rolling Year {rollingStart:yyyy-MM-dd}",
						StartDate = rollingStart,
						EndDate = rollingEnd,
						DurationDays = (rollingEnd - rollingStart).TotalDays
					});
				}
				rollingStart = rollingStart.AddMonths(3);
			}

			return periods;
		}

		private bool HasActivitiesInPeriod(List<DateTime> activityDates, DateTime start, DateTime end)
		{
			return activityDates.Any(a => a >= start && a <= end);
		}

		private string GetPeriodCategory(string periodName)
		{
			if (periodName.Contains("Week")) return "Weekly Periods";
			if (periodName.Contains("Month") && !periodName.Contains("Year") && !periodName.Contains("First")) return "Monthly Periods";
			if (periodName.Contains("Quarter") || periodName.StartsWith("Q")) return "Quarterly Periods";
			if (periodName.Contains("Year") && !periodName.Contains("Rolling")) return "Annual Periods";
			if (periodName.Contains("Rolling")) return "Rolling Periods";
			if (periodName.Contains("Inception")) return "Inception Periods";
			if (periodName.Contains("First")) return "Milestone Periods";
			return "Other Periods";
		}

		private int GetCategoryOrder(string category)
		{
			return category switch
			{
				"Weekly Periods" => 1,
				"Monthly Periods" => 2,
				"Quarterly Periods" => 3,
				"Annual Periods" => 4,
				"Rolling Periods" => 5,
				"Milestone Periods" => 6,
				"Inception Periods" => 7,
				_ => 8
			};
		}

		#endregion
	}
}

#region Data Models

public class TimePeriodInfo
{
	public string Name { get; set; } = string.Empty;
	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }
	public double DurationDays { get; set; }
}

public class PortfolioStatistics
{
	public int TotalActivities { get; set; }
	public int TotalHoldings { get; set; }
	public int TotalAccounts { get; set; }
	public int UniqueSymbols { get; set; }
	public DateTime FirstActivityDate { get; set; }
	public DateTime LastActivityDate { get; set; }
	public double PortfolioAgeDays { get; set; }
	public Dictionary<string, int> ActivitiesByType { get; set; } = new();
}

#endregion