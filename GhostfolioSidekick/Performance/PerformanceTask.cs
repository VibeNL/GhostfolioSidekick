using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PerformanceCalculations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Performance
{
	internal class PerformanceTask(
		IHoldingPerformanceCalculator holdingPerformanceCalculator,
		IDbContextFactory<DatabaseContext> dbContextFactory) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.PerformanceCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Performance Calculations";

		public async Task DoWork(ILogger logger)
		{
			// Calculate performance for all holdings
			var holdings = await holdingPerformanceCalculator.GetCalculatedHoldings();

			if (holdings == null || !holdings.Any())
			{
				await DeleteAllHoldings();
				return;
			}

			// Use a fresh context for obsolete detection
			using (var dbContext = dbContextFactory.CreateDbContext())
			{
				var existingHoldingKeys = await dbContext.HoldingAggregateds
					.Select(h => new { h.Id, h.Symbol, h.AssetClass, h.AssetSubClass })
					.AsNoTracking()
					.ToListAsync();

				var newHoldingKeys = holdings
					.Select(h => (h.Symbol, h.AssetClass, h.AssetSubClass))
					.ToHashSet();

				var obsoleteIds = existingHoldingKeys
					.Where(existing => !newHoldingKeys.Contains((existing.Symbol, existing.AssetClass, existing.AssetSubClass)))
					.Select(existing => existing.Id)
					.ToList();

				if (obsoleteIds.Count != 0)
				{
					var toDelete = await dbContext.HoldingAggregateds
						.Where(h => obsoleteIds.Contains(h.Id))
						.ToListAsync();
					dbContext.HoldingAggregateds.RemoveRange(toDelete);
					await dbContext.SaveChangesAsync();
				}
			}

			// Process holdings in batches with a fresh context per batch
			const int batchSize = 1;
			var holdingList = holdings.ToList();
			foreach (var batch in holdingList.Chunk(batchSize))
			{
				using var dbContext = dbContextFactory.CreateDbContext();
				var batchKeys = batch.Select(h => (h.Symbol, h.AssetClass, h.AssetSubClass)).ToHashSet();

				var existingBatchHoldings = (await dbContext.HoldingAggregateds
					.Include(h => h.CalculatedSnapshots)
					.Where(h => batch.Select(b => b.Symbol).Contains(h.Symbol))
					.ToListAsync())
					.Where(h => batchKeys.Contains((h.Symbol, h.AssetClass, h.AssetSubClass)))
					.ToList();

				foreach (var holding in batch)
				{
					var existing = existingBatchHoldings
						.FirstOrDefault(h =>
							h.Symbol == holding.Symbol &&
							h.AssetClass == holding.AssetClass &&
							h.AssetSubClass == holding.AssetSubClass);

					if (existing != null)
					{
						UpdateExistingHolding(existing, holding);
					}
					else
					{
						await dbContext.HoldingAggregateds.AddAsync(holding);
					}
				}

				await dbContext.SaveChangesAsync();
			}
		}

		private async Task DeleteAllHoldings()
		{
			using var dbContext = dbContextFactory.CreateDbContext();
			var allHoldings = await dbContext.HoldingAggregateds.ToListAsync();
			if (allHoldings.Count != 0)
			{
				dbContext.HoldingAggregateds.RemoveRange(allHoldings);
				await dbContext.SaveChangesAsync();
			}
		}

		private static void UpdateExistingHolding(HoldingAggregated existing, HoldingAggregated holding)
		{
			// Update HoldingAggregated properties
			existing.Name = holding.Name;
			existing.AssetClass = holding.AssetClass;
			existing.AssetSubClass = holding.AssetSubClass;
			existing.ActivityCount = holding.ActivityCount;
			existing.CountryWeight = holding.CountryWeight;
			existing.SectorWeights = holding.SectorWeights;
			existing.DataSource = holding.DataSource;

			UpdateCalculatedSnapshots(existing, holding.CalculatedSnapshots);
		}

		private static void UpdateCalculatedSnapshots(HoldingAggregated existing, ICollection<CalculatedSnapshot> newSnapshots)
		{
			// Use tuple for snapshot keys
			var existingSnapshotsByKey = existing.CalculatedSnapshots.ToDictionary(s => (s.AccountId, s.Date));
			var newSnapshotsByKey = newSnapshots.ToDictionary(s => (s.AccountId, s.Date));

			// Remove snapshots that no longer exist in the new calculation
			var snapshotsToRemove = existing.CalculatedSnapshots
				.Where(existingSnapshot => !newSnapshotsByKey.ContainsKey((existingSnapshot.AccountId, existingSnapshot.Date)))
				.ToList();

			foreach (var snapshotToRemove in snapshotsToRemove)
			{
				existing.CalculatedSnapshots.Remove(snapshotToRemove);
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
					existing.CalculatedSnapshots.Add(newSnapshot);
				}
			}
		}

		private static void UpdateSnapshotProperties(CalculatedSnapshot existingSnapshot, CalculatedSnapshot newSnapshot)
		{
			existingSnapshot.Quantity = newSnapshot.Quantity;
			existingSnapshot.AverageCostPrice = newSnapshot.AverageCostPrice;
			existingSnapshot.CurrentUnitPrice = newSnapshot.CurrentUnitPrice;
			existingSnapshot.TotalInvested = newSnapshot.TotalInvested;
			existingSnapshot.TotalValue = newSnapshot.TotalValue;
		}
	}
}
