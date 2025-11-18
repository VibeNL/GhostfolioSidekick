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
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IApplicationSettings applicationSettings
		) : IScheduledWork
	{
		const int batchSize = 1000;

		public TaskPriority Priority => TaskPriority.CovertToPrimaryCurrency;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Convert to Primary Currency";

		public async Task DoWork(ILogger logger)
		{
			var primaryCurrencySymbol = applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency;
			var currency = Currency.GetCurrency(primaryCurrencySymbol);

			await currencyExchange.PreloadAllExchangeRates();

			logger.LogDebug("Converting all snapshots and balances to primary currency {Currency}", primaryCurrencySymbol);

			await ConvertSnapshotsToPrimaryCurrency(currency, primaryCurrencySymbol, logger);
			await ConvertBalancesToPrimaryCurrency(currency, primaryCurrencySymbol, logger);

			logger.LogDebug("Adding missing days and extrapolating balances to today");
			await FillMissingDaysAndExtrapolate(logger);

			logger.LogDebug("Cleanup unmatched primary currency records");
			await CleanupUnmatchedItems();

			logger.LogDebug("Completed conversion to primary currency {Currency}", primaryCurrencySymbol);
		}

		private async Task ConvertSnapshotsToPrimaryCurrency(Currency currency, string primaryCurrencySymbol, ILogger logger)
		{
			var totalSnapshots = 0;
			using (var dbContext = dbContextFactory.CreateDbContext())
			{
				totalSnapshots = await dbContext.CalculatedSnapshots.CountAsync();
			}

			int processed = 0;

			for (int i = 0; i < totalSnapshots; i += batchSize)
			{
				using var dbContext = dbContextFactory.CreateDbContext();

				var snapshots = await dbContext.CalculatedSnapshots
					.AsNoTracking()
					.OrderBy(s => s.Id)
					.Skip(i)
					.Take(batchSize)
					.ToListAsync();

				foreach (var snapshot in snapshots)
				{
					var primarySnapshot = await dbContext.CalculatedSnapshotPrimaryCurrencies
						.FirstOrDefaultAsync(s => s.HoldingAggregatedId == snapshot.HoldingAggregatedId && s.AccountId == snapshot.AccountId && s.Date == snapshot.Date);

					if (primarySnapshot == null)
					{
						primarySnapshot = new CalculatedSnapshotPrimaryCurrency
						{
							HoldingAggregatedId = snapshot.HoldingAggregatedId,
							Date = snapshot.Date
						};
						dbContext.CalculatedSnapshotPrimaryCurrencies.Add(primarySnapshot);
					}

					primarySnapshot.Quantity = snapshot.Quantity;
					primarySnapshot.TotalValue = (await currencyExchange.ConvertMoney(snapshot.TotalValue, currency, snapshot.Date)).Amount;
					primarySnapshot.TotalInvested = (await currencyExchange.ConvertMoney(snapshot.TotalInvested, currency, snapshot.Date)).Amount;
					primarySnapshot.AverageCostPrice = primarySnapshot.Quantity != 0 ? primarySnapshot.TotalInvested / primarySnapshot.Quantity : 0;
					primarySnapshot.CurrentUnitPrice = (await currencyExchange.ConvertMoney(snapshot.CurrentUnitPrice, currency, snapshot.Date)).Amount;
					primarySnapshot.AccountId = snapshot.AccountId;
				}

				await dbContext.SaveChangesAsync();

				processed += snapshots.Count;
				logger.LogDebug("Processed {Processed}/{Total} snapshots to primary currency {Currency}", processed, totalSnapshots, primaryCurrencySymbol);
			}

			logger.LogDebug("Converted {Count} snapshots to primary currency {Currency}", totalSnapshots, primaryCurrencySymbol);
		}

		private async Task ConvertBalancesToPrimaryCurrency(Currency currency, string primaryCurrencySymbol, ILogger logger)
		{
			var balanceIds = new List<int>();
			using (var dbContext = dbContextFactory.CreateDbContext())
			{
				balanceIds = await dbContext.Balances
					.AsNoTracking()
					.Select(b => b.Id)
					.ToListAsync();
			}

			foreach (var chunk in balanceIds.Chunk(batchSize))
			{
				using var dbContext = dbContextFactory.CreateDbContext();

				var balances = await dbContext.Balances
					.Where(b => chunk.Contains(b.Id))
					.AsNoTracking()
					.ToListAsync();

				foreach (var balance in balances)
				{
					var primaryBalance = await dbContext.BalancePrimaryCurrencies
						.FirstOrDefaultAsync(b => b.AccountId == balance.AccountId && b.Date == balance.Date);

					if (primaryBalance == null)
					{
						primaryBalance = new BalancePrimaryCurrency
						{
							AccountId = balance.AccountId,
							Date = balance.Date
						};
						dbContext.BalancePrimaryCurrencies.Add(primaryBalance);
					}

					primaryBalance.Money = (await currencyExchange.ConvertMoney(balance.Money, currency, balance.Date)).Amount;
				}

				await dbContext.SaveChangesAsync();
			}

			logger.LogDebug("Converted {Count} balances to primary currency {Currency}", balanceIds.Count, primaryCurrencySymbol);
		}

		private async Task FillMissingDaysAndExtrapolate(ILogger logger)
		{
			using var dbContext = dbContextFactory.CreateDbContext();
			var accountIds = await dbContext.BalancePrimaryCurrencies
				.Select(b => b.AccountId)
				.Distinct()
				.ToListAsync();

			var today = DateOnly.FromDateTime(DateTime.Today);

			foreach (var accountId in accountIds)
			{
				var existingBalances = await dbContext.BalancePrimaryCurrencies
					.Where(b => b.AccountId == accountId)
					.OrderBy(b => b.Date)
					.AsNoTracking()
					.ToListAsync();

				if (existingBalances.Count == 0)
				{
					continue;
				}

				var startDate = existingBalances[0].Date;
				var lastKnownDate = existingBalances[^1].Date;
				var lastKnownAmount = existingBalances[^1].Money;

				var existingDates = existingBalances.Select(b => b.Date).ToHashSet();
				var newBalances = new List<BalancePrimaryCurrency>();

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
					dbContext.BalancePrimaryCurrencies.AddRange(newBalances);
					await dbContext.SaveChangesAsync();
					logger.LogDebug("Added {Count} missing balance records for account {AccountId}", newBalances.Count, accountId);
				}
			}
		}

		private async Task CleanupUnmatchedItems()
		{
			using var dbContext = dbContextFactory.CreateDbContext();
			var orphanedPrimarySnapshots = await dbContext.CalculatedSnapshotPrimaryCurrencies
				.Where(ps => !dbContext.CalculatedSnapshots
					.Any(s => s.HoldingAggregatedId == ps.HoldingAggregatedId &&
							  s.AccountId == ps.AccountId &&
							  s.Date == ps.Date))
				.ToListAsync();

			if (orphanedPrimarySnapshots.Count > 0)
			{
				dbContext.CalculatedSnapshotPrimaryCurrencies.RemoveRange(orphanedPrimarySnapshots);
			}

			var firstActualBalanceDates = await dbContext.Balances
				.GroupBy(b => b.AccountId)
				.Select(g => new { AccountId = g.Key, FirstDate = g.Min(b => b.Date) })
				.ToListAsync();

			var firstActualBalanceDateDict = firstActualBalanceDates.ToDictionary(x => x.AccountId, x => x.FirstDate);

			var orphanedPrimaryBalances = await dbContext.BalancePrimaryCurrencies
				.Where(pb => !dbContext.Balances
					.Any(b => b.AccountId == pb.AccountId && b.Date == pb.Date))
				.ToListAsync();

			var balancesToRemove = orphanedPrimaryBalances
				.Where(pb => firstActualBalanceDateDict.TryGetValue(pb.AccountId, out var firstActualDate) && pb.Date < firstActualDate)
				.ToList();

			if (balancesToRemove.Count > 0)
			{
				dbContext.BalancePrimaryCurrencies.RemoveRange(balancesToRemove);
			}

			if (orphanedPrimarySnapshots.Count > 0 || balancesToRemove.Count > 0)
			{
				await dbContext.SaveChangesAsync();
			}
		}
	}
}
