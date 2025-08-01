﻿using GhostfolioSidekick.Database;
using GhostfolioSidekick.PerformanceCalculations;
using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Performance
{
	internal class PerformanceTask(
		IHoldingPerformanceCalculator holdingPerformanceCalculator,
		DatabaseContext databaseContext) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.PerformanceCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			// Calculate performance for all holdings
			var holdings = await holdingPerformanceCalculator.GetCalculatedHoldings();

			if (holdings == null || !holdings.Any())
			{
				return;
			}

			// Update holdings and their snapshots
			foreach (var holding in holdings)
			{
				// Try to find existing entity with its snapshots
				var existing = await databaseContext.HoldingAggregateds
					.Include(h => h.CalculatedSnapshots)
					.FirstOrDefaultAsync(h => 
						h.Symbol == holding.Symbol && 
						h.AssetClass == holding.AssetClass && 
						h.AssetSubClass == holding.AssetSubClass);

				if (existing != null)
				{
					UpdateExistingHolding(existing, holding);
				}
				else
				{
					await databaseContext.HoldingAggregateds.AddAsync(holding);
				}
			}

			await databaseContext.SaveChangesAsync();
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
			var existingSnapshotsByDate = existing.CalculatedSnapshots.ToDictionary(s => s.Date);
			var newSnapshotsByDate = newSnapshots.ToDictionary(s => s.Date);

			// Remove snapshots that no longer exist in the new calculation
			var snapshotsToRemove = existing.CalculatedSnapshots
				.Where(existingSnapshot => !newSnapshotsByDate.ContainsKey(existingSnapshot.Date))
				.ToList();
			
			foreach (var snapshotToRemove in snapshotsToRemove)
			{
				existing.CalculatedSnapshots.Remove(snapshotToRemove);
			}

			// Update existing snapshots or add new ones
			foreach (var newSnapshot in newSnapshots)
			{
				if (existingSnapshotsByDate.TryGetValue(newSnapshot.Date, out var existingSnapshot))
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
