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
		private const int SaveBatchSize = 10;

		public TaskPriority Priority => TaskPriority.PerformanceCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Performance Calculations";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Starting performance calculations for holdings...");
			var currency = Currency.GetCurrency(applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency) ?? Currency.EUR;

			// Remove all snapshots and load holding IDs in a single context
			List<int> holdingIds;
			using (var dbContext = await dbContextFactory.CreateDbContextAsync())
			{
				await dbContext.CalculatedSnapshots.ExecuteDeleteAsync();

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

			logger.LogInformation("Total holdings to process: {Total}", totalHoldings);

			// Reuse a single DbContext across holdings and batch saves every SaveBatchSize holdings
			using var batchDbContext = await dbContextFactory.CreateDbContextAsync();

			for (int i = 0; i < holdingIds.Count; i++)
			{
				var holdingId = holdingIds[i];
				try
				{
					var holding = await batchDbContext.Holdings
						.FirstOrDefaultAsync(h => h.Id == holdingId);

					if (holding == null)
					{
						logger.LogWarning("Holding {HoldingId} not found, skipping", holdingId);
					}
					else
					{
						// Calculate new snapshots for this holding
						var newSnapshots = (await performanceCalculator.GetCalculatedSnapshots(holding, currency)).ToList();

						foreach (var newSnapshot in newSnapshots)
						{
							newSnapshot.Id = Guid.NewGuid();
							newSnapshot.HoldingId = holding.Id;
							batchDbContext.CalculatedSnapshots.Add(newSnapshot);
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error calculating performance for holding {HoldingId}", holdingId);

					// Detach any tracked entities that may be in a bad state to allow the batch to continue
					foreach (var entry in batchDbContext.ChangeTracker.Entries().ToList())
					{
						entry.State = EntityState.Detached;
					}
				}

				processedHoldings++;
				logger.LogInformation("Processed {Processed}/{Total} holdings", processedHoldings, totalHoldings);

				// Flush every SaveBatchSize holdings or on the last one
				bool isLastHolding = i == holdingIds.Count - 1;
				if ((processedHoldings % SaveBatchSize == 0) || isLastHolding)
				{
					await batchDbContext.SaveChangesAsync();
				}
			}

			logger.LogInformation("Performance calculation completed for {Count} holdings", totalHoldings);
		}
	}
}
