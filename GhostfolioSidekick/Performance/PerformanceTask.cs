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

		public TimeSpan? MaxRunTime => null;

		public async Task DoWork(ILogger logger, CancellationToken cancellationToken)
		{
			logger.LogInformation("Starting performance calculations for holdings...");
			var currency = Currency.GetCurrency(applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency) ?? Currency.EUR;

			// Remove all snapshots and load holding IDs in a single context
			List<int> holdingIds;
			using (var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken))
			{
				await dbContext.CalculatedSnapshots.ExecuteDeleteAsync(cancellationToken: cancellationToken);

				holdingIds = await dbContext.Holdings
					.Where(h => h.SymbolProfiles.Any())
					.Select(h => h.Id)
					.ToListAsync(cancellationToken);
			}

			if (holdingIds == null || holdingIds.Count == 0)
			{
				logger.LogInformation("No holdings found to calculate performance for");
				return;
			}

			int totalHoldings = holdingIds.Count;
			int processedHoldings = 0;

			logger.LogInformation("Total holdings to process: {Total}", totalHoldings);


			for (int i = 0; i < holdingIds.Count; i++)
			{
				using var batchDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

				var holdingId = holdingIds[i];
				try
				{
					var holding = await batchDbContext.Holdings
						.FirstOrDefaultAsync(h => h.Id == holdingId, cancellationToken: cancellationToken);

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

						// Set Quantity on DividendActivity for this holding using the latest snapshot
						var latestQuantity = newSnapshots
							.OrderByDescending(s => s.Date)
							.Select(s => s.Quantity)
							.FirstOrDefault();

						if (latestQuantity > 0)
						{
							var dividendActivities = batchDbContext.Activities
								.OfType<GhostfolioSidekick.Model.Activities.Types.DividendActivity>()
								.Where(a => a.Holding != null && a.Holding.Id == holding.Id);

							foreach (var activity in dividendActivities)
							{
								activity.Quantity = latestQuantity;
							}
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error calculating performance for holding {HoldingId}", holdingId);
				}

				processedHoldings++;
				logger.LogInformation("Processed {Processed}/{Total} holdings", processedHoldings, totalHoldings);

				try
				{
					await batchDbContext.SaveChangesAsync(cancellationToken);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error saving calculated snapshots batch (up to holding {HoldingId})", holdingId);
				}
			}

			logger.LogInformation("Performance calculation completed for {Count} holdings", totalHoldings);
		}
	}
}
