using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PerformanceCalculations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Performance
{
	internal class PerformanceTask(
		IPerformanceCalculator performanceCalculator,
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IApplicationSettings applicationSettings) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.PerformanceCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Performance Calculations";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Starting performance calculations for holdings...");
			var currency = Currency.GetCurrency(applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency) ?? Currency.EUR;

			// Fetch all holding IDs with SymbolProfiles
			List<int> holdingIds;
			using (var dbContext = await dbContextFactory.CreateDbContextAsync())
			{
				holdingIds = await dbContext.Holdings
					.Where(h => h.SymbolProfiles.Any())
					.Select(h => h.Id)
					.ToListAsync();
			}

			if (holdingIds == null || holdingIds.Count == 0)
			{
				logger.LogInformation("No holdings found to calculate performance for");
				return;
			}

			int totalHoldings = holdingIds.Count;
			int processedHoldings = 0;
			const int batchSize = 20; // Tune this for optimal memory/DB performance

			logger.LogInformation("Total holdings to process: {Total}", totalHoldings);

			for (int i = 0; i < holdingIds.Count; i += batchSize)
			{
				var batchIds = holdingIds.Skip(i).Take(batchSize).ToList();
				using var dbContext = await dbContextFactory.CreateDbContextAsync();

				// Fetch holdings and their snapshots in one query
				var holdings = await dbContext.Holdings
					.Include(h => h.CalculatedSnapshots)
					.Where(h => batchIds.Contains(h.Id))
					.ToListAsync();

				// Remove old snapshots for these holdings in bulk
				var snapshotIdsToDelete = holdings.SelectMany(h => h.CalculatedSnapshots.Select(s => s.Id)).ToList();
				if (snapshotIdsToDelete.Count > 0)
				{
					var snapshotsToDelete = dbContext.CalculatedSnapshots.Where(s => snapshotIdsToDelete.Contains(s.Id));
					await snapshotsToDelete.ExecuteDeleteAsync();
				}

				var allNewSnapshots = new List<GhostfolioSidekick.Model.Performance.CalculatedSnapshot>();

				foreach (var holding in holdings)
				{
					try
					{
						var newSnapshots = (await performanceCalculator.GetCalculatedSnapshots(holding, currency)).ToList();
						foreach (var newSnapshot in newSnapshots)
						{
							newSnapshot.Id = Guid.NewGuid();
							newSnapshot.HoldingId = holding.Id;
						}
						allNewSnapshots.AddRange(newSnapshots);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Error calculating performance for holding {HoldingId}", holding.Id);
					}
					processedHoldings++;
					logger.LogInformation("Processed {Processed}/{Total} holdings", processedHoldings, totalHoldings);
				}

				if (allNewSnapshots.Count > 0)
				{
					dbContext.CalculatedSnapshots.AddRange(allNewSnapshots);
					await dbContext.SaveChangesAsync();
				}
			}

			logger.LogInformation("Performance calculation completed for {Count} holdings", totalHoldings);
		}
	}
}
