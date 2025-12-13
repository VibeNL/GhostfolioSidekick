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

			logger.LogDebug("Cleanup unmatched primary currency records");
			await CleanupUnmatchedItems();

			logger.LogDebug("Completed conversion to primary currency {Currency}", primaryCurrencySymbol);
		}

		private async Task ConvertSnapshotsToPrimaryCurrency(Currency currency, string primaryCurrencySymbol, ILogger logger)
		{
			var totalSnapshots = 0;
			using (var dbContext = await dbContextFactory.CreateDbContextAsync())
			{
				totalSnapshots = await dbContext.CalculatedSnapshots.CountAsync();
			}

			int processed = 0;

			for (int i = 0; i < totalSnapshots; i += batchSize)
			{
				using var dbContext = await dbContextFactory.CreateDbContextAsync();

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
			using var queryContext = await dbContextFactory.CreateDbContextAsync();
			var accountIds = await queryContext.Balances
				.AsNoTracking()
				.Select(b => b.AccountId)
				.Distinct()
				.ToListAsync();

			var today = DateOnly.FromDateTime(DateTime.Today);

			foreach (var accountId in accountIds)
			{
				using var dbContext = await dbContextFactory.CreateDbContextAsync();

				var balances = await dbContext.Balances
					.Where(b => b.AccountId == accountId)
					.OrderBy(b => b.Date)
					.AsNoTracking()
					.ToListAsync();

				if (balances.Count == 0)
				{
					continue;
				}

				var startDate = balances[0].Date;
				var balanceByDate = balances.ToDictionary(b => b.Date);
				var existingPrimary = await dbContext.BalancePrimaryCurrencies
					.Where(b => b.AccountId == accountId)
					.ToDictionaryAsync(b => b.Date);

				decimal lastKnownAmount = 0;
				for (var date = startDate; date <= today; date = date.AddDays(1))
				{
					if (balanceByDate.TryGetValue(date, out var balance))
					{
						var converted = await currencyExchange.ConvertMoney(balance.Money, currency, date);
						lastKnownAmount = converted.Amount;
					}

					if (!existingPrimary.TryGetValue(date, out BalancePrimaryCurrency? value))
					{
						dbContext.BalancePrimaryCurrencies.Add(new BalancePrimaryCurrency
						{
							AccountId = accountId,
							Date = date,
							Money = lastKnownAmount
						});
					}
					else
					{
						value.Money = lastKnownAmount;
					}
				}

				await dbContext.SaveChangesAsync();
			}

			logger.LogDebug("Converted and filled balances to primary currency {Currency}", primaryCurrencySymbol);
		}

		private async Task CleanupUnmatchedItems()
		{
			using var dbContext = await dbContextFactory.CreateDbContextAsync();
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
