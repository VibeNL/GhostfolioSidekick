using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Performance
{
	internal class CovertToPrimaryCurrencyTask(
		ICurrencyExchange currencyExchange,
		DatabaseContext databaseContext,
		IApplicationSettings applicationSettings,
		ILogger<CovertToPrimaryCurrencyTask> logger
		) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CovertToPrimaryCurrency;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			var primaryCurrencySymbol = applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency;
			var currency = Currency.GetCurrency(primaryCurrencySymbol);

			await currencyExchange.PreloadAllExchangeRates();

			logger.LogInformation("Converting all snapshots and balances to primary currency {Currency}", primaryCurrencySymbol);

			var snapshots = databaseContext.CalculatedSnapshots.AsQueryable();

			foreach (var snapshot in snapshots)
			{
				var primarySnapshot = await databaseContext.CalculatedSnapshotPrimaryCurrencies
					.FirstOrDefaultAsync(s => s.HoldingAggregatedId == snapshot.HoldingAggregatedId && s.AccountId == snapshot.AccountId && s.Date == snapshot.Date);

				if (primarySnapshot == null)
				{
					primarySnapshot = new CalculatedSnapshotPrimaryCurrency
					{
						HoldingAggregatedId = snapshot.HoldingAggregatedId,
						Date = snapshot.Date
					};
					databaseContext.CalculatedSnapshotPrimaryCurrencies.Add(primarySnapshot);
				}

				primarySnapshot.Quantity = snapshot.Quantity;
				primarySnapshot.TotalValue = (await currencyExchange.ConvertMoney(snapshot.TotalValue, currency, snapshot.Date)).Amount;
				primarySnapshot.TotalInvested = (await currencyExchange.ConvertMoney(snapshot.TotalInvested, currency, snapshot.Date)).Amount;
				primarySnapshot.AverageCostPrice = primarySnapshot.Quantity != 0 ? primarySnapshot.TotalInvested / primarySnapshot.Quantity : 0;
				primarySnapshot.CurrentUnitPrice = (await currencyExchange.ConvertMoney(snapshot.CurrentUnitPrice, currency, snapshot.Date)).Amount;
				primarySnapshot.AccountId = snapshot.AccountId;
			}

			logger.LogDebug("Converted {Count} snapshots to primary currency {Currency}", await snapshots.CountAsync(), primaryCurrencySymbol);

			await databaseContext.SaveChangesAsync();

			logger.LogDebug("Converting all balances to primary currency {Currency}", primaryCurrencySymbol);

			var balances = databaseContext.Balances.AsQueryable();

			foreach (var balance in balances)
			{
				var primaryBalance = await databaseContext.BalancePrimaryCurrencies
					.FirstOrDefaultAsync(b => b.AccountId == balance.AccountId && b.Date == balance.Date);

				if (primaryBalance == null)
				{
					primaryBalance = new BalancePrimaryCurrency
					{
						AccountId = balance.AccountId,
						Date = balance.Date
					};
					databaseContext.BalancePrimaryCurrencies.Add(primaryBalance);
				}

				primaryBalance.Money = (await currencyExchange.ConvertMoney(balance.Money, currency, balance.Date)).Amount;
			}

			logger.LogDebug("Converted {Count} balances to primary currency {Currency}", await balances.CountAsync(), primaryCurrencySymbol);

			await databaseContext.SaveChangesAsync();

			logger.LogDebug("Adding missing days and extrapolating balances to today");

			// Fill missing days and extrapolate to today for each account
			await FillMissingDaysAndExtrapolate();

			logger.LogDebug("Cleanup unmatched primary currency records");

			// Cleanup unmatched items (but preserve extrapolated records)
			await CleanupUnmatchedItems();

			logger.LogInformation("Completed conversion to primary currency {Currency}", primaryCurrencySymbol);
		}

		private async Task FillMissingDaysAndExtrapolate()
		{
			var accountIds = await databaseContext.BalancePrimaryCurrencies
				.Select(b => b.AccountId)
				.Distinct()
				.ToListAsync();

			var today = DateOnly.FromDateTime(DateTime.Today);

			foreach (var accountId in accountIds)
			{
				var existingBalances = await databaseContext.BalancePrimaryCurrencies
					.Where(b => b.AccountId == accountId)
					.OrderBy(b => b.Date)
					.ToListAsync();

				if (existingBalances.Count == 0)
				{
					continue;
				}

				var startDate = existingBalances.First().Date;
				var lastKnownDate = existingBalances.Last().Date;
				var lastKnownAmount = existingBalances.Last().Money;

				// Create set of existing dates for quick lookup
				var existingDates = existingBalances.Select(b => b.Date).ToHashSet();
				var newBalances = new List<BalancePrimaryCurrency>();

				// Fill missing days between first and last known dates
				for (var date = startDate; date <= lastKnownDate; date = date.AddDays(1))
				{
					if (!existingDates.Contains(date))
					{
						var previousAmount = existingBalances.LastOrDefault(b => b.Date < date)?.Money ?? 0;
						newBalances.Add(new BalancePrimaryCurrency
						{
							AccountId = accountId,
							Date = date,
							Money = previousAmount
						});
					}
				}

				// Extrapolate from last known date to today
				for (var date = lastKnownDate.AddDays(1); date <= today; date = date.AddDays(1))
				{
					newBalances.Add(new BalancePrimaryCurrency
					{
						AccountId = accountId,
						Date = date,
						Money = lastKnownAmount
					});
				}

				if (newBalances.Count != 0)
				{
					databaseContext.BalancePrimaryCurrencies.AddRange(newBalances);
					logger.LogInformation("Added {Count} missing balance records for account {AccountId}", newBalances.Count, accountId);
				}
			}

			await databaseContext.SaveChangesAsync();
		}

		private async Task CleanupUnmatchedItems()
		{
			// Clean up CalculatedSnapshotPrimaryCurrency records that no longer have corresponding CalculatedSnapshot records
			var orphanedPrimarySnapshots = await databaseContext.CalculatedSnapshotPrimaryCurrencies
				.Where(ps => !databaseContext.CalculatedSnapshots
					.Any(s => s.HoldingAggregatedId == ps.HoldingAggregatedId && 
							  s.AccountId == ps.AccountId && 
							  s.Date == ps.Date))
				.ToListAsync();

			if (orphanedPrimarySnapshots.Count > 0)
			{
				databaseContext.CalculatedSnapshotPrimaryCurrencies.RemoveRange(orphanedPrimarySnapshots);
			}

			// Only clean up BalancePrimaryCurrency records that don't have corresponding Balance records
			// AND are before the first actual balance date (preserve filled-in and extrapolated records)
			var firstActualBalanceDates = await databaseContext.Balances
				.GroupBy(b => b.AccountId)
				.Select(g => new { AccountId = g.Key, FirstDate = g.Min(b => b.Date) })
				.ToListAsync();

			var firstActualBalanceDateDict = firstActualBalanceDates.ToDictionary(x => x.AccountId, x => x.FirstDate);

			var orphanedPrimaryBalances = await databaseContext.BalancePrimaryCurrencies
				.Where(pb => !databaseContext.Balances
					.Any(b => b.AccountId == pb.AccountId && b.Date == pb.Date))
				.ToListAsync();

			// Only remove records that are before the first actual balance date for each account
			var balancesToRemove = orphanedPrimaryBalances
				.Where(pb => firstActualBalanceDateDict.TryGetValue(pb.AccountId, out var firstActualDate) && pb.Date < firstActualDate)
				.ToList();

			if (balancesToRemove.Count > 0)
			{
				databaseContext.BalancePrimaryCurrencies.RemoveRange(balancesToRemove);
			}

			// Save all cleanup changes
			if (orphanedPrimarySnapshots.Count > 0 || balancesToRemove.Count > 0)
			{
				await databaseContext.SaveChangesAsync();
			}
		}
	}
}
