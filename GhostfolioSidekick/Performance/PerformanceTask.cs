using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
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
		const int batchSize = 10;

		public TaskPriority Priority => TaskPriority.PerformanceCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Performance Calculations";

		public async Task DoWork(ILogger logger)
		{
			var currency = Currency.GetCurrency(applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency) ?? Currency.EUR;
			using var dbContext = await dbContextFactory.CreateDbContextAsync();

			// Get all holdings from the database
			var holdings = await dbContext.Holdings
				.Include(h => h.CalculatedSnapshots)
				.ToListAsync();

			if (holdings == null || holdings.Count == 0)
			{
				logger.LogInformation("No holdings found to calculate performance for");
				return;
			}

			// Process holdings in batches
			foreach (var batch in holdings.Chunk(batchSize))
			{
				foreach (var holding in batch)
				{
					try
					{
						// Calculate new snapshots for this holding
						var newSnapshots = (await performanceCalculator.GetCalculatedSnapshots(holding, currency)).ToList();

						// Update the holding's calculated snapshots
						await UpdateCalculatedSnapshotsAsync(holding, newSnapshots, dbContext);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Error calculating performance for holding {HoldingId}", holding.Id);
					}
				}

				// Save changes for this batch
				await dbContext.SaveChangesAsync();
			}

			logger.LogInformation("Performance calculation completed for {Count} holdings", holdings.Count);
		}

		private static async Task UpdateCalculatedSnapshotsAsync(Model.Holding holding, ICollection<CalculatedSnapshot> newSnapshots, DatabaseContext dbContext)
		{
			// Use tuple for snapshot keys (AccountId, Date)
			var existingSnapshotsByKey = holding.CalculatedSnapshots.ToDictionary(s => (s.AccountId, s.Date));
			var newSnapshotsByKey = newSnapshots.ToDictionary(s => (s.AccountId, s.Date));

			// Remove snapshots that no longer exist in the new calculation
			var snapshotsToRemove = holding.CalculatedSnapshots
				.Where(existingSnapshot => !newSnapshotsByKey.ContainsKey((existingSnapshot.AccountId, existingSnapshot.Date)))
				.ToList();

			foreach (var snapshotToRemove in snapshotsToRemove)
			{
				holding.CalculatedSnapshots.Remove(snapshotToRemove);
				dbContext.CalculatedSnapshots.Remove(snapshotToRemove);
			}

			// Save removals before adding new snapshots to avoid unique constraint violation
			if (snapshotsToRemove.Count > 0)
			{
				await dbContext.SaveChangesAsync();
			}

			// Update existing snapshots or add new ones
			foreach (var newSnapshot in newSnapshots)
			{
				if (existingSnapshotsByKey.TryGetValue((newSnapshot.AccountId, newSnapshot.Date), out var existingSnapshot))
				{
					UpdateSnapshotProperties(existingSnapshot, newSnapshot);
				}
				else
				{
					// Set the HoldingId for new snapshots
					newSnapshot.HoldingId = holding.Id;
					holding.CalculatedSnapshots.Add(newSnapshot);
				}
			}
		}

		private static void UpdateSnapshotProperties(CalculatedSnapshot existingSnapshot, CalculatedSnapshot newSnapshot)
		{
			existingSnapshot.Quantity = newSnapshot.Quantity;
			existingSnapshot.Currency = newSnapshot.Currency;
			existingSnapshot.AverageCostPrice = newSnapshot.AverageCostPrice;
			existingSnapshot.CurrentUnitPrice = newSnapshot.CurrentUnitPrice;
			existingSnapshot.TotalInvested = newSnapshot.TotalInvested;
			existingSnapshot.TotalValue = newSnapshot.TotalValue;
		}
	}
}
