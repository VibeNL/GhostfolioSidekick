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
				primarySnapshot.CurrentUnitPrice = primarySnapshot.Quantity != 0 ? primarySnapshot.TotalValue / primarySnapshot.Quantity : 0;
				primarySnapshot.AccountId = snapshot.AccountId;
			}

			logger.LogInformation("Converted {Count} snapshots to primary currency {Currency}", await snapshots.CountAsync(), primaryCurrencySymbol);

			await databaseContext.SaveChangesAsync();

			logger.LogInformation("Converting all balances to primary currency {Currency}", primaryCurrencySymbol);

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

			logger.LogInformation("Converted {Count} balances to primary currency {Currency}", await balances.CountAsync(), primaryCurrencySymbol);

			await databaseContext.SaveChangesAsync();

			logger.LogInformation("Cleanup unmatched primary currency records");

			// Cleanup unmatched items
			await CleanupUnmatchedItems();

			logger.LogInformation("Completed conversion to primary currency {Currency}", primaryCurrencySymbol);
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

			// Clean up BalancePrimaryCurrency records that no longer have corresponding Balance records
			var orphanedPrimaryBalances = await databaseContext.BalancePrimaryCurrencies
				.Where(pb => !databaseContext.Balances
					.Any(b => b.AccountId == pb.AccountId && b.Date == pb.Date))
				.ToListAsync();

			if (orphanedPrimaryBalances.Count > 0)
			{
				databaseContext.BalancePrimaryCurrencies.RemoveRange(orphanedPrimaryBalances);
			}

			// Save all cleanup changes
			if (orphanedPrimarySnapshots.Count > 0 || orphanedPrimaryBalances.Count > 0)
			{
				await databaseContext.SaveChangesAsync();
			}
		}
	}
}
