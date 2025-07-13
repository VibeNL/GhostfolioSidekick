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
		/// Get the latest performance snapshot for a specific period and scope
		/// </summary>
		public async Task<PortfolioPerformance?> GetLatestPerformanceAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType,
			PerformanceScope scope = PerformanceScope.Portfolio,
			string? scopeIdentifier = null)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				var snapshot = await context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.CalculationType == calculationType &&
							   s.Scope == scope &&
							   s.ScopeIdentifier == scopeIdentifier &&
							   s.IsLatest)
					.FirstOrDefaultAsync();

				if (snapshot != null)
				{
					logger.LogDebug("Found performance snapshot for period {StartDate} to {EndDate}, scope {Scope}:{ScopeId}, type {CalculationType}",
						startDate, endDate, scope, scopeIdentifier ?? "All", calculationType);
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
			PortfolioPerformance performance,
			PerformanceScope scope = PerformanceScope.Portfolio,
			string? scopeIdentifier = null)
		{
			try
			{
				var portfolioHash = GeneratePortfolioHash(holdings, startDate, endDate, scope, scopeIdentifier);

				using var context = await dbContextFactory.CreateDbContextAsync();

				// Mark existing snapshots for the same period and scope as not latest
				var existingSnapshots = await context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.CalculationType == calculationType &&
							   s.Scope == scope &&
							   s.ScopeIdentifier == scopeIdentifier)
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
					Scope = scope,
					ScopeIdentifier = scopeIdentifier,
					Metadata = JsonSerializer.Serialize(new
					{
						Scope = scope.ToString(),
						ScopeId = scopeIdentifier,
						HoldingCount = holdings.Count,
						ActivityCount = holdings.SelectMany(h => h.Activities).Count(),
						CalculationTimestamp = DateTime.UtcNow,
						PortfolioComposition = holdings.Select(h => new
						{
							Symbol = h.SymbolProfiles.FirstOrDefault()?.Symbol ?? "Unknown",
							ActivityCount = h.Activities.Count,
							Account = h.Activities.FirstOrDefault()?.Account?.Name ?? "Unknown"
						}).ToList()
					})
				};

				context.PortfolioPerformanceSnapshots.Add(snapshot);
				await context.SaveChangesAsync();

				logger.LogInformation("Stored performance snapshot for period {StartDate} to {EndDate}, scope {Scope}:{ScopeId}, type {CalculationType}, version {Version}",
					startDate, endDate, scope, scopeIdentifier ?? "All", calculationType, newVersion);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error storing performance snapshot");
				throw;
			}
		}

		/// <summary>
		/// Get performance snapshots by scope (all accounts or all assets)
		/// </summary>
		public async Task<List<PortfolioPerformanceSnapshot>> GetPerformanceByScope(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			PerformanceScope scope,
			string? calculationType = null)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				var query = context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.Scope == scope &&
							   s.IsLatest);

				if (!string.IsNullOrEmpty(calculationType))
				{
					query = query.Where(s => s.CalculationType == calculationType);
				}

				return await query.OrderBy(s => s.ScopeIdentifier).ToListAsync();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error retrieving performance snapshots by scope");
				return new List<PortfolioPerformanceSnapshot>();
			}
		}

		/// <summary>
		/// Get all available account performances for a period
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> GetAccountPerformancesAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType = "MarketData")
		{
			var snapshots = await GetPerformanceByScope(startDate, endDate, baseCurrency, PerformanceScope.Account, calculationType);
			
			return snapshots.ToDictionary(
				s => s.ScopeIdentifier ?? "Unknown",
				s => s.Performance);
		}

		/// <summary>
		/// Get all available asset performances for a period
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> GetAssetPerformancesAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType = "MarketData")
		{
			var snapshots = await GetPerformanceByScope(startDate, endDate, baseCurrency, PerformanceScope.Asset, calculationType);
			
			return snapshots.ToDictionary(
				s => s.ScopeIdentifier ?? "Unknown",
				s => s.Performance);
		}

		/// <summary>
		/// Check if performance needs recalculation based on portfolio changes
		/// </summary>
		public async Task<bool> NeedsRecalculationAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType,
			PerformanceScope scope = PerformanceScope.Portfolio,
			string? scopeIdentifier = null)
		{
			try
			{
				var currentHash = GeneratePortfolioHash(holdings, startDate, endDate, scope, scopeIdentifier);

				using var context = await dbContextFactory.CreateDbContextAsync();

				var latestSnapshot = await context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.CalculationType == calculationType &&
							   s.Scope == scope &&
							   s.ScopeIdentifier == scopeIdentifier &&
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
		/// Get historical performance snapshots for a period and scope
		/// </summary>
		public async Task<List<PortfolioPerformanceSnapshot>> GetPerformanceHistoryAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string? calculationType = null,
			PerformanceScope scope = PerformanceScope.Portfolio,
			string? scopeIdentifier = null)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				var query = context.PortfolioPerformanceSnapshots
					.Where(s => s.StartDate == startDate &&
							   s.EndDate == endDate &&
							   s.BaseCurrency == baseCurrency &&
							   s.Scope == scope &&
							   s.ScopeIdentifier == scopeIdentifier);

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
		/// Get all available performance periods by scope
		/// </summary>
		public async Task<List<(DateTime StartDate, DateTime EndDate, Currency BaseCurrency, string CalculationType, PerformanceScope Scope, string? ScopeIdentifier)>> GetAvailablePeriodsAsync()
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				return await context.PortfolioPerformanceSnapshots
					.Where(s => s.IsLatest)
					.Select(s => new { s.StartDate, s.EndDate, s.BaseCurrency, s.CalculationType, s.Scope, s.ScopeIdentifier })
					.Distinct()
					.Select(s => ValueTuple.Create(s.StartDate, s.EndDate, s.BaseCurrency, s.CalculationType, s.Scope, s.ScopeIdentifier))
					.ToListAsync();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error retrieving available periods");
				return new List<(DateTime, DateTime, Currency, string, PerformanceScope, string?)>();
			}
		}

		/// <summary>
		/// Clean up old versions, keeping only the latest N versions for each period/scope combination
		/// </summary>
		public async Task CleanupOldVersionsAsync(int versionsToKeep = 5)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				// Group by period, calculation type, and scope
				var periods = await context.PortfolioPerformanceSnapshots
					.GroupBy(s => new { s.StartDate, s.EndDate, s.BaseCurrency, s.CalculationType, s.Scope, s.ScopeIdentifier })
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

				var scopeCounts = await context.PortfolioPerformanceSnapshots
					.GroupBy(s => s.Scope)
					.Select(g => new { Scope = g.Key, Count = g.Count() })
					.ToListAsync();

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
					SnapshotsByCalculationType = periodCounts.ToDictionary(p => p.CalculationType, p => p.Count),
					SnapshotsByScope = scopeCounts.ToDictionary(s => s.Scope.ToString(), s => s.Count)
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
		private string GeneratePortfolioHash(List<Holding> holdings, DateTime startDate, DateTime endDate, PerformanceScope scope, string? scopeIdentifier)
		{
			// Filter holdings based on scope
			var relevantHoldings = scope switch
			{
				PerformanceScope.Account => holdings.Where(h => 
					h.Activities.Any(a => a.Account?.Name == scopeIdentifier)).ToList(),
				PerformanceScope.Asset => holdings.Where(h => 
					h.SymbolProfiles.Any(sp => sp.Symbol == scopeIdentifier)).ToList(),
				_ => holdings
			};

			// Create a stable representation of the portfolio for hashing
			var portfolioData = new
			{
				Scope = scope.ToString(),
				ScopeId = scopeIdentifier,
				Holdings = relevantHoldings.Select(h => new
				{
					h.Id,
					SymbolProfile = h.SymbolProfiles.FirstOrDefault()?.Symbol ?? "",
					Activities = h.Activities
						.Where(a => a.Date >= startDate && a.Date <= endDate)
						.Where(a => scope != PerformanceScope.Account || a.Account?.Name == scopeIdentifier)
						.OrderBy(a => a.Date)
						.Select(a => new
						{
							a.Date,
							a.GetType().Name,
							Account = a.Account?.Name,
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
		public Dictionary<string, int> SnapshotsByScope { get; set; } = new();
	}
}