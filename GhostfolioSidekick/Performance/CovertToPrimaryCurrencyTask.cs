using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Performance
{
	internal class CovertToPrimaryCurrencyTask(
		ICurrencyExchange currencyExchange,
		DatabaseContext databaseContext,
		IApplicationSettings applicationSettings
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

			await databaseContext.SaveChangesAsync();

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

			await databaseContext.SaveChangesAsync();
		}
	}
}
