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
		public TaskPriority Priority => TaskPriority.PerformanceCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Performance Calculations";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Starting performance calculations for holdings...");
			var currency = Currency.GetCurrency(applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency) ?? Currency.EUR;

			// Remove all snapshots at the start
			using (var dbContext = await dbContextFactory.CreateDbContextAsync())
			{
				await dbContext.CalculatedSnapshots.ExecuteDeleteAsync();
			}

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

					logger.LogInformation("Total holdings to process: {Total}", totalHoldings);

						foreach (var holdingId in holdingIds)
						{
							using var dbContext = await dbContextFactory.CreateDbContextAsync();
							try
							{
								var holding = await dbContext.Holdings
									.Include(h => h.CalculatedSnapshots)
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
										dbContext.CalculatedSnapshots.Add(newSnapshot);
									}

									await dbContext.SaveChangesAsync();
								}
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Error calculating performance for holding {HoldingId}", holdingId);
							}

							processedHoldings++;
							logger.LogInformation("Processed {Processed}/{Total} holdings", processedHoldings, totalHoldings);
						}

						logger.LogInformation("Performance calculation completed for {Count} holdings", totalHoldings);
					}
				}
			}
