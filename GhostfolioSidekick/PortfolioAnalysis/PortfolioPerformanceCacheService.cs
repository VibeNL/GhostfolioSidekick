using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Service for storing and retrieving portfolio performance calculation snapshots
	/// </summary>
	public class PortfolioPerformanceStorageService
	{
		private readonly IDbContextFactory<DatabaseContext> dbContextFactory;
		private readonly ILogger<PortfolioPerformanceStorageService> logger;

		public PortfolioPerformanceStorageService(
			IDbContextFactory<DatabaseContext> dbContextFactory,
			ILogger<PortfolioPerformanceStorageService> logger)
		{
			this.dbContextFactory = dbContextFactory;
			this.logger = logger;
		}

		/// <summary>
		/// Get the latest performance snapshot for a specific period
		/// </summary>
		public async Task<PortfolioPerformance?> GetLatestPerformanceAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				var snapshot = await context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.CalculationType == calculationType &&
							   s.IsLatest)
					.FirstOrDefaultAsync();

				if (snapshot != null)
				{
					logger.LogDebug("Found performance snapshot for period {StartDate} to {EndDate} with type {CalculationType}",
						startDate, endDate, calculationType);
					return snapshot.Performance;
				}

				return null;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error retrieving performance snapshot");
				return null;
			}
		}

		/// <summary>
		/// Store a new performance calculation snapshot
		/// </summary>
		public async Task StorePerformanceAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType,
			PortfolioPerformance performance)
		{
			try
			{
				var portfolioHash = GeneratePortfolioHash(holdings, startDate, endDate);

				using var context = await dbContextFactory.CreateDbContextAsync();

				// Mark existing snapshots for the same period as not latest
				var existingSnapshots = await context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.CalculationType == calculationType)
					.ToListAsync();

				foreach (var existing in existingSnapshots)
				{
					existing.IsLatest = false;
				}

				// Create new snapshot
				var newVersion = existingSnapshots.Any() ? existingSnapshots.Max(s => s.Version) + 1 : 1;

				var snapshot = new PortfolioPerformanceSnapshot
				{
					PortfolioHash = portfolioHash,
					StartDate = startDate,
					EndDate = endDate,
					BaseCurrency = baseCurrency,
					CalculationType = calculationType,
					Performance = performance,
					CalculatedAt = DateTime.UtcNow,
					Version = newVersion,
					IsLatest = true,
					Metadata = JsonSerializer.Serialize(new
					{
						HoldingCount = holdings.Count,
						ActivityCount = holdings.SelectMany(h => h.Activities).Count(),
						CalculationTimestamp = DateTime.UtcNow,
						PortfolioComposition = holdings.Select(h => new
						{
							Symbol = h.SymbolProfiles.FirstOrDefault()?.Symbol ?? "Unknown",
							ActivityCount = h.Activities.Count
						}).ToList()
					})
				};

				context.PortfolioPerformanceSnapshots.Add(snapshot);
				await context.SaveChangesAsync();

				logger.LogInformation("Stored performance snapshot for period {StartDate} to {EndDate} with type {CalculationType}, version {Version}",
					startDate, endDate, calculationType, newVersion);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error storing performance snapshot");
				throw;
			}
		}

		/// <summary>
		/// Get historical performance snapshots for a period
		/// </summary>
		public async Task<List<PortfolioPerformanceSnapshot>> GetPerformanceHistoryAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string? calculationType = null)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				var query = context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency);

				if (!string.IsNullOrEmpty(calculationType))
				{
					query = query.Where(s => s.CalculationType == calculationType);
				}

				return await query
					.OrderByDescending(s => s.CalculatedAt)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error retrieving performance history");
				return new List<PortfolioPerformanceSnapshot>();
			}
		}

		/// <summary>
		/// Get all available performance periods
		/// </summary>
		public async Task<List<(DateTime StartDate, DateTime EndDate, Currency BaseCurrency, string CalculationType)>> GetAvailablePeriodsAsync()
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				return await context.PortfolioPerformanceSnapshots
					.Where(s => s.IsLatest)
					.Select(s => new { s.StartDate, s.EndDate, s.BaseCurrency, s.CalculationType })
					.Distinct()
					.Select(s => ValueTuple.Create(s.StartDate, s.EndDate, s.BaseCurrency, s.CalculationType))
					.ToListAsync();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error retrieving available periods");
				return new List<(DateTime, DateTime, Currency, string)>();
			}
		}

		/// <summary>
		/// Check if performance needs recalculation based on portfolio changes
		/// </summary>
		public async Task<bool> NeedsRecalculationAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType)
		{
			try
			{
				var currentHash = GeneratePortfolioHash(holdings, startDate, endDate);

				using var context = await dbContextFactory.CreateDbContextAsync();

				var latestSnapshot = await context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.CalculationType == calculationType &&
							   s.IsLatest)
					.FirstOrDefaultAsync();

				// If no snapshot exists or portfolio has changed, recalculation is needed
				return latestSnapshot == null || latestSnapshot.PortfolioHash != currentHash;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error checking if recalculation is needed");
				return true; // Default to recalculation if we can't determine
			}
		}

		/// <summary>
		/// Clean up old versions, keeping only the latest N versions for each period
		/// </summary>
		public async Task CleanupOldVersionsAsync(int versionsToKeep = 5)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				// Group by period and calculation type
				var periods = await context.PortfolioPerformanceSnapshots
					.GroupBy(s => new { s.StartDate, s.EndDate, s.BaseCurrency, s.CalculationType })
					.ToListAsync();

				int totalRemoved = 0;

				foreach (var periodGroup in periods)
				{
					var snapshots = periodGroup
						.OrderByDescending(s => s.CalculatedAt)
						.ToList();

					if (snapshots.Count > versionsToKeep)
					{
						var toRemove = snapshots.Skip(versionsToKeep).ToList();
						context.PortfolioPerformanceSnapshots.RemoveRange(toRemove);
						totalRemoved += toRemove.Count;
					}
				}

				if (totalRemoved > 0)
				{
					await context.SaveChangesAsync();
					logger.LogInformation("Cleaned up {Count} old performance snapshot versions", totalRemoved);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error cleaning up old versions");
			}
		}

		/// <summary>
		/// Get storage statistics
		/// </summary>
		public async Task<PerformanceStorageStatistics> GetStorageStatisticsAsync()
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				var totalSnapshots = await context.PortfolioPerformanceSnapshots.CountAsync();
				var latestSnapshots = await context.PortfolioPerformanceSnapshots.CountAsync(s => s.IsLatest);
				var oldestSnapshot = await context.PortfolioPerformanceSnapshots
					.MinAsync(s => (DateTime?)s.CalculatedAt);
				var newestSnapshot = await context.PortfolioPerformanceSnapshots
					.MaxAsync(s => (DateTime?)s.CalculatedAt);

				var periodCounts = await context.PortfolioPerformanceSnapshots
					.GroupBy(s => s.CalculationType)
					.Select(g => new { CalculationType = g.Key, Count = g.Count() })
					.ToListAsync();

				return new PerformanceStorageStatistics
				{
					TotalSnapshots = totalSnapshots,
					LatestSnapshots = latestSnapshots,
					HistoricalSnapshots = totalSnapshots - latestSnapshots,
					OldestSnapshot = oldestSnapshot,
					NewestSnapshot = newestSnapshot,
					SnapshotsByCalculationType = periodCounts.ToDictionary(p => p.CalculationType, p => p.Count)
				};
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error getting storage statistics");
				return new PerformanceStorageStatistics();
			}
		}

		/// <summary>
		/// Generate a hash of the portfolio composition for detecting changes
		/// </summary>
		private string GeneratePortfolioHash(List<Holding> holdings, DateTime startDate, DateTime endDate)
		{
			// Create a stable representation of the portfolio for hashing
			var portfolioData = new
			{
				Holdings = holdings.Select(h => new
				{
					h.Id,
					SymbolProfile = h.SymbolProfiles.FirstOrDefault()?.Symbol ?? "",
					Activities = h.Activities
						.Where(a => a.Date >= startDate && a.Date <= endDate)
						.OrderBy(a => a.Date)
						.Select(a => new
						{
							a.Date,
							a.GetType().Name,
							// Add relevant activity properties for comparison
							ActivityData = a.ToString() // Simplified for now
						})
				}).OrderBy(h => h.SymbolProfile),
				StartDate = startDate,
				EndDate = endDate
			};

			var json = JsonSerializer.Serialize(portfolioData, new JsonSerializerOptions
			{
				WriteIndented = false,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});

			using var sha256 = SHA256.Create();
			var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
			return Convert.ToHexString(hash);
		}
	}

	/// <summary>
	/// Storage statistics for monitoring
	/// </summary>
	public class PerformanceStorageStatistics
	{
		public int TotalSnapshots { get; set; }
		public int LatestSnapshots { get; set; }
		public int HistoricalSnapshots { get; set; }
		public DateTime? OldestSnapshot { get; set; }
		public DateTime? NewestSnapshot { get; set; }
		public Dictionary<string, int> SnapshotsByCalculationType { get; set; } = new();
	}
}